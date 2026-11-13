namespace AStarDev.CloudSync.Sync;

internal sealed record DeletionScan(IReadOnlyList<LocalChange> Unreadable, IReadOnlyList<PendingDeletion> Pending, IReadOnlyDictionary<SyncPairId, int> Totals);
