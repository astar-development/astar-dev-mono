using System.Collections.Concurrent;
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
    /// Streams <paramref name="jobs"/> through the parallel pipeline, enqueuing each to the repository
    /// as it arrives, and persists a <see cref="SyncedItemEntity"/> for each successfully completed download or upload.
    /// The <paramref name="mappings"/> are loaded once by the caller and passed in to avoid a redundant DB round-trip.
    /// Progress and job-completion events are forwarded via the provided callbacks.
    /// Returns the number of jobs that failed.
    /// </summary>
    Task<int> ExecuteAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, IAsyncEnumerable<SyncJob> jobs, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, IReadOnlyList<FileClassificationCategory> mappings, Action<SyncProgressEventArgs> onProgress, Func<JobCompletedEventArgs, Task> onJobCompleted, CancellationToken ct);
}
