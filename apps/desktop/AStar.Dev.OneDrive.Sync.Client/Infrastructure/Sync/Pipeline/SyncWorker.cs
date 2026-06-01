using System.Threading.Channels;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>
/// Drains jobs from a <see cref="ChannelReader{T}"/> and dispatches each to the
/// appropriate <see cref="IJobHandler"/>. Multiple workers run concurrently.
/// </summary>
public sealed class SyncWorker(int workerId, IReadOnlyList<IJobHandler> handlers, ISyncRepository syncRepository, ILogger<SyncWorker> logger) : ISyncWorker
{
    /// <inheritdoc />
    public async Task RunAsync(ChannelReader<SyncJob> reader, string accountId, Func<CancellationToken, Task<string>> tokenFactory, Action<SyncJob, bool, string?> onJobComplete, CancellationToken ct)
    {
        await foreach (var job in reader.ReadAllAsync(ct))
        {
            ct.ThrowIfCancellationRequested();

            OneDriveSyncClientMessages.SyncWorkerProcessing(logger, workerId, job.GetType().Name, job.Target.RelativePath);

            await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.InProgress, Option.None<string>()).ConfigureAwait(false);

            var currentJob = job;
            string? error = null;
            bool success = false;

            try
            {
                (currentJob, success, error) = await ExecuteJobAsync(job, accountId, tokenFactory, ct)
                    .MatchAsync<SyncJob, string, (SyncJob, bool, string?)>(
                        completedJob => (completedJob, true, null),
                        reason => (currentJob, false, reason)).ConfigureAwait(false);

                if (success)
                    await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Completed, Option.None<string>()).ConfigureAwait(false);
                else
                    await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Failed, (Option<string>)error!).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Queued, Option.None<string>()).ConfigureAwait(false);
                throw;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                OneDriveSyncClientMessages.SyncWorkerException(logger, workerId, ex.GetType().Name, ex.Message, job.Target.LocalPath, ex);
                await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Failed, (Option<string>)ex.Message).ConfigureAwait(false);
            }
            finally
            {
                onJobComplete(currentJob, success, error);
            }
        }
    }

    private Task<Result<SyncJob, string>> ExecuteJobAsync(SyncJob job, string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken ct)
    {
        var handler = handlers.FirstOrDefault(h => h.CanHandle(job));

        if (handler is null)
        {
            OneDriveSyncClientMessages.SyncWorkerNoHandler(logger, workerId, job.GetType().Name);

            return Task.FromResult<Result<SyncJob, string>>(new Result<SyncJob, string>.Error($"No handler registered for job type '{job.GetType().Name}'."));
        }

        return handler.HandleAsync(job, accountId, tokenFactory, ct);
    }
}
