using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncAttentionItem"/> cases.</summary>
public static class SyncAttentionItemFactory
{
    /// <summary>Creates the <see cref="SyncAttentionItem.Conflict"/> case.</summary>
    /// <param name="id">The conflict's id.</param>
    /// <param name="accountId">The account.</param>
    /// <param name="path">The conflicted path.</param>
    /// <param name="kind">Why it is a conflict.</param>
    /// <param name="local">The local side now.</param>
    /// <param name="remote">The cloud side now.</param>
    /// <param name="detectedAt">When it was detected.</param>
    public static SyncAttentionItem CreateConflict(ConflictId id, AccountId accountId, RelativeSyncPath path, ConflictKind kind, SideSnapshot local, SideSnapshot remote, DateTimeOffset detectedAt) =>
        new SyncAttentionItem.Conflict(id, accountId, path, kind, local, remote, detectedAt);

    /// <summary>Creates the <see cref="SyncAttentionItem.HeldDeletions"/> case. Empty paths are dropped, duplicates ignoring case are merged and the rest are sorted ignoring case.</summary>
    /// <param name="id">The held set's id.</param>
    /// <param name="accountId">The account.</param>
    /// <param name="pairId">The sync pair.</param>
    /// <param name="direction">Which side the deletions would be applied to.</param>
    /// <param name="paths">The held paths.</param>
    /// <param name="detectedAt">When the hold was recorded.</param>
    public static SyncAttentionItem CreateHeldDeletions(HeldDeletionId id, AccountId accountId, SyncPairId pairId, DeletionDirection direction, IEnumerable<RelativeSyncPath> paths, DateTimeOffset detectedAt) =>
        new SyncAttentionItem.HeldDeletions(id, accountId, pairId, direction, [.. (paths ?? []).Where(path => !string.IsNullOrEmpty(path.Value)).Distinct().OrderBy(path => path.Value, StringComparer.OrdinalIgnoreCase)], detectedAt);
}
