using System.Threading.Channels;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Orchestrates parallel file downloads using a bounded Channel.
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
public sealed class ParallelDownloadPipeline(ISyncRepository syncRepository, IGraphService graphService, IHttpDownloader downloader, int workerCount = 8) : IDisposable
{
    private readonly Lock _lock = new();

    public async Task RunAsync(IEnumerable<SyncJob> jobs, string accessToken, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs> onJobCompleted, string accountId, string folderId, CancellationToken ct = default)
    {
        var jobList = jobs.ToList();
        if(jobList.Count == 0)
            return;

        int total   = jobList.Count;
        int done    = 0;

        var channel = Channel.CreateBounded<SyncJob>(
            new BoundedChannelOptions(workerCount * 4)
            {
                FullMode     = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true
            });

        void OnJobComplete(SyncJob job, bool success, string? error)
        {
            int completedSoFar;
            lock(_lock)
            {
                done++;
                completedSoFar = done;
            }

            var completedJob = job with
            {
                State        = success ? SyncJobState.Completed : SyncJobState.Failed,
                ErrorMessage = error,
                CompletedAt  = DateTimeOffset.UtcNow
            };

            bool isComplete = completedSoFar == total;

            onProgress(new SyncProgressEventArgs(accountId: accountId, folderId: folderId, completed: completedSoFar, total: total, currentFile: job.RelativePath, syncState: completedSoFar == total ? SyncState.Idle : SyncState.Syncing));

            onJobCompleted(new JobCompletedEventArgs(completedJob));
        }

        var workers = Enumerable.Range(1, workerCount)
            .Select(id => new DownloadWorker(                    id, downloader, graphService, syncRepository)
            .RunAsync(channel.Reader, accessToken, OnJobComplete, ct))
            .ToList();

        try
        {
            foreach(var job in jobList)
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
            Serilog.Log.Information("[Pipeline] All workers completed normally");
        }
        catch(Exception ex)
        {
            Serilog.Log.Error(ex, "[Pipeline] Worker threw unhandled exception: {Type} {Error}", ex.GetType().Name, ex.Message);
        }
        finally
        {
            onProgress(new SyncProgressEventArgs(accountId: accountId, folderId: folderId, completed: done, total: total, currentFile: string.Empty, syncState: SyncState.Idle));

            Serilog.Log.Information("[Pipeline] Final progress raised — done={Done} total={Total}", done, total);
        }

        await syncRepository.ClearCompletedJobsAsync(accountId);

        Serilog.Log.Information("[Pipeline] Complete — {Done}/{Total} jobs processed", done, total);
    }

    public void Dispose() => downloader.Dispose();
}
