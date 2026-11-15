using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncPair"/> values.</summary>
public static class SyncPairFactory
{
    /// <summary>Creates a sync pair. Missing text is normalised to an empty string.</summary>
    public static SyncPair Create(SyncPairId id, AccountId accountId, string name, LocalRelativePath localFolder, Option<RemoteItemId> remoteFolder, SyncDirection direction, SyncPairState state) => new(id, accountId, name ?? string.Empty, localFolder, remoteFolder, direction, state.Status, state.FileCount, state.LastSync);

    /// <summary>Creates the same sync pair with another state.</summary>
    /// <param name="pair">The pair to copy.</param>
    /// <param name="state">The status, file count and last sync the new pair shows.</param>
    public static SyncPair WithState(SyncPair pair, SyncPairState state) => Create(pair.Id, pair.AccountId, pair.Name, pair.LocalFolder, pair.RemoteFolder, pair.Direction, state);
}
