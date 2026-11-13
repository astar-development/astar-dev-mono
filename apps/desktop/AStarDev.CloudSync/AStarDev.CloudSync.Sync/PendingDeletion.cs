using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

internal sealed record PendingDeletion(RelativeSyncPath Path, RemoteItemId ItemId, SyncPairId Pair);
