using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncPair"/> values.</summary>
public static class SyncPairFactory
{
    /// <summary>Creates a sync pair. Missing text is normalised to an empty string.</summary>
    public static SyncPair Create(SyncPairId id, AccountId accountId, string name, LocalRelativePath localFolder, Option<RemoteItemId> remoteFolder, SyncDirection direction, SyncPairState state) => new(id, accountId, name ?? string.Empty, localFolder, remoteFolder, direction, state.Status, state.FileCount, state.LastSync);
}
