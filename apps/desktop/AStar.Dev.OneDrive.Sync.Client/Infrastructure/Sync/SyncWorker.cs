using System.Threading.Channels;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Drains jobs from a <see cref="ChannelReader{T}"/> and dispatches each to the
/// appropriate <see cref="IJobHandler"/>. Multiple workers run concurrently.
/// </summary>
public sealed class SyncWorker(int workerId, IReadOnlyList<IJobHandler> handlers, ISyncRepository syncRepository) : ISyncWorker
{
    /// <inheritdoc />
    public async Task RunAsync(ChannelReader<SyncJob> reader, string accessToken, Action<SyncJob, bool, string?> onJobComplete, CancellationToken ct)
    {
        await foreach(var job in reader.ReadAllAsync(ct))
        {
            ct.ThrowIfCancellationRequested();

            Serilog.Log.Debug("[Worker {Id}] Processing {JobType} {Path}", workerId, job.GetType().Name, job.Target.RelativePath);

            await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.InProgress).ConfigureAwait(false);

            var currentJob = job;
            string? error = null;
            bool success = false;

            try
            {
                (currentJob, success, error) = await ExecuteJobAsync(job, accessToken, ct)
                    .MatchAsync<SyncJob, string, (SyncJob, bool, string?)>(
                        completedJob => (completedJob, true, null),
                        reason => (currentJob, false, reason)).ConfigureAwait(false);

                if(success)
                    await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Completed).ConfigureAwait(false);
                else
                    await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Failed, error).ConfigureAwait(false);
            }
            catch(OperationCanceledException)
            {
                await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Queued).ConfigureAwait(false);
                throw;
            }
            catch(Exception ex)
            {
                error = ex.Message;
                Serilog.Log.Error(ex, "[Worker {Id}] EXCEPTION type={Type} message={Error} path={Path}", workerId, ex.GetType().Name, ex.Message, job.Target.LocalPath);
                await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Failed, ex.Message).ConfigureAwait(false);
            }
            finally
            {
                onJobComplete(currentJob, success, error);
            }
        }
    }

    private Task<Result<SyncJob, string>> ExecuteJobAsync(SyncJob job, string accessToken, CancellationToken ct)
    {
        var handler = handlers.FirstOrDefault(h => h.CanHandle(job));

        if(handler is null)
        {
            Serilog.Log.Warning("[Worker {Id}] No handler registered for job type {JobType}", workerId, job.GetType().Name);

            return Task.FromResult<Result<SyncJob, string>>(new Result<SyncJob, string>.Error($"No handler registered for job type '{job.GetType().Name}'."));
        }

        return handler.HandleAsync(job, accessToken, ct);
    }
}
