using System.Collections.Concurrent;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;


namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;

/// <summary>
/// Detects remote items that were deleted since the last sync and removes their local counterparts.
/// </summary>
public interface IRemoteDeletionDetector
{
    /// <summary>
    /// Cross-references <paramref name="syncedItems"/> against <paramref name="seenRemoteIds"/> from
    /// the current enumeration pass. For each absent remote ID, deletes the local file or directory
    /// and removes the tracking record from the repository.
    /// Returns the items removed locally; a deleted folder is reported once, not once per descendant.
    /// </summary>
    Task<IReadOnlyList<SyncedItemEntity>> DetectAndApplyAsync(AccountId accountId, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, IReadOnlySet<string> seenRemoteIds, IReadOnlyList<SyncRuleEntity> rules, CancellationToken cancellationToken);
}
