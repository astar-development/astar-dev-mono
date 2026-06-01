using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>
/// Enqueues sync jobs, runs them through the parallel pipeline, and records the resulting state.
/// </summary>
public interface ISyncJobExecutor
{
    /// <summary>
    /// Enqueues <paramref name="jobs"/> to the repository, runs them through the parallel pipeline,
    /// and persists a <see cref="SyncedItemEntity"/> for each successfully completed download or upload.
    /// Progress and job-completion events are forwarded via the provided callbacks.
    /// </summary>
    Task ExecuteAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, IReadOnlyList<SyncJob> jobs, Dictionary<string, SyncedItemEntity> syncedItems, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs> onJobCompleted, CancellationToken ct);
}
