using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

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
    Task ExecuteAsync(OneDriveAccount account, string accessToken, IReadOnlyList<SyncJob> jobs, Dictionary<string, SyncedItemEntity> syncedItems, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs> onJobCompleted, CancellationToken ct);
}
