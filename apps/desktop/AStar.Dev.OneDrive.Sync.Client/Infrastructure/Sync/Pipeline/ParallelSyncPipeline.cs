using System.Threading.Channels;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>
/// Orchestrates parallel file sync using a bounded Channel.
///
/// Architecture:
///   Producer  — feeds SyncJob items into the channel one at a time,
///               applying backpressure when workers are saturated.
///   Consumers — N workers drain the channel concurrently.
///
/// Backpressure: the channel capacity is (Workers × 4) so the producer
/// never loads more than ~4 jobs per worker into memory at once.
/// With 300k files this means memory stays flat regardless of job count.
/// </summary>
public sealed class ParallelSyncPipeline(ISyncWorkerFactory workerFactory, ISyncRepository syncRepository, ILogger<ParallelSyncPipeline> logger, IOptions<SyncSettings> syncSettings) : ISyncPipeline
{
    /// <inheritdoc />
    public async Task RunAsync(IEnumerable<SyncJob> jobs, Func<CancellationToken, Task<string>> tokenFactory, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs> onJobCompleted, string accountId, string folderId, int workerCount = 4, CancellationToken ct = default)
    {
        var jobList = jobs.ToList();
        if (jobList.Count == 0)
            return;

        var tracker = new SyncProgressTracker(jobList.Count, accountId, folderId, syncSettings.Value.ProgressReportInterval);

        var channel = Channel.CreateBounded<SyncJob>(
            new BoundedChannelOptions(workerCount * 4)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true
            });

        var workers = Enumerable.Range(1, workerCount)
            .Select(id => workerFactory.Create(id).RunAsync(channel.Reader, accountId, tokenFactory, (job, success, error) => tracker.RecordCompletion(job, success, error, onProgress, onJobCompleted), ct))
            .ToList();

        try
        {
            foreach (var job in jobList)
            {
                ct.ThrowIfCancellationRequested();
                await channel.Writer.WriteAsync(job, ct);
            }
        }
        finally
        {
            channel.Writer.Complete();
        }

        try
        {
            await Task.WhenAll(workers);
            OneDriveSyncClientMessages.SyncPipelineCompleted(logger);
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.SyncPipelineWorkerException(logger, ex.GetType().Name, ex.Message, ex);
        }
        finally
        {
            onProgress(new SyncProgressEventArgs(accountId: accountId, folderId: folderId, completed: tracker.Done, total: jobList.Count, currentFile: string.Empty, syncState: SyncState.Idle));
            OneDriveSyncClientMessages.SyncPipelineFinalProgress(logger, tracker.Done, jobList.Count);
        }

        await syncRepository.ClearCompletedJobsAsync(new AccountId(accountId));

        OneDriveSyncClientMessages.SyncPipelineJobsProcessed(logger, tracker.Done, jobList.Count);
    }
}
