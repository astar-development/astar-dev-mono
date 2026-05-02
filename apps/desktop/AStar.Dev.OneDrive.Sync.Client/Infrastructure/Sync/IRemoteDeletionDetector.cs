using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;


namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Detects remote items that were deleted since the last sync and removes their local counterparts.
/// </summary>
public interface IRemoteDeletionDetector
{
    /// <summary>
    /// Cross-references <paramref name="syncedItems"/> against <paramref name="seenRemoteIds"/> from
    /// the current enumeration pass. For each absent remote ID, deletes the local file or directory
    /// and removes the tracking record from the repository.
    /// </summary>
    Task DetectAndApplyAsync(AccountId accountId, Dictionary<string, SyncedItemEntity> syncedItems, IReadOnlySet<string> seenRemoteIds, IReadOnlyList<SyncRuleEntity> rules, CancellationToken ct);
}
