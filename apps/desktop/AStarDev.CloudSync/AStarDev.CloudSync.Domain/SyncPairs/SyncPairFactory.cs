using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Core.Common;
using AStarDev.CloudSync.Core.SyncPairs;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Domain.SyncPairs;

/// <summary>Creates <see cref="SyncPair"/> values.</summary>
public static class SyncPairFactory
{
    /// <summary>Creates a sync pair. Missing text is normalised to empty strings and a negative file count to zero.</summary>
    public static SyncPair Create(SyncPairId id, AccountId accountId, string name, DirectoryInfo localFolder, string remotePath, SyncDirection direction, SyncStatus status, int fileCount, Option<DateTimeOffset> lastSync) => new(id, accountId, name ?? string.Empty, localFolder, remotePath ?? string.Empty, direction, status, Math.Max(fileCount, 0), lastSync);
}
