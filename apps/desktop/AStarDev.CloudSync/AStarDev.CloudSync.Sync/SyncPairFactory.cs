using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncPair"/> values.</summary>
public static class SyncPairFactory
{
    /// <summary>Creates a sync pair. Missing text is normalised to an empty string and a negative file count to zero.</summary>
    public static SyncPair Create(SyncPairId id, AccountId accountId, string name, LocalRelativePath localFolder, Option<RemoteItemId> remoteFolder, SyncDirection direction, SyncStatus status, int fileCount, Option<DateTimeOffset> lastSync) => new(id, accountId, name ?? string.Empty, localFolder, remoteFolder, direction, status, Math.Max(fileCount, 0), lastSync);
}
