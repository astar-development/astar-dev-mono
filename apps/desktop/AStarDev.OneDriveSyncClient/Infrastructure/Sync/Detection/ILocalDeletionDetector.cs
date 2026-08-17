using System.Collections.Concurrent;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;

/// <summary>
/// Detects locally deleted files and propagates those deletions to the remote drive.
/// </summary>
public interface ILocalDeletionDetector
{
    /// <summary>
    /// Walks <paramref name="syncedItems"/> and, for each file no longer present on disk,
    /// deletes the corresponding remote item via Graph and removes the local tracking record.
    /// </summary>
    Task DetectAndApplyAsync(AccountId accountId, Func<CancellationToken, Task<string>> tokenFactory, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, CancellationToken cancellationToken);
}
