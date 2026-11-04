using AStarDev.CloudSync.Core.Conflicts;
using AStarDev.CloudSync.Core.SyncPairs;

namespace AStarDev.CloudSync.Domain.Conflicts;

/// <summary>Creates <see cref="SyncConflict"/> values.</summary>
public static class SyncConflictFactory
{
    /// <summary>Creates a conflict. A missing file name is normalised to an empty string.</summary>
    public static SyncConflict Create(SyncPairId syncPairId, string fileName, DateTimeOffset detectedAt) => new(syncPairId, fileName ?? string.Empty, detectedAt);
}
