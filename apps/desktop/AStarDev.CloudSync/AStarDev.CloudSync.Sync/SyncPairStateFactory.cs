using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncPairState"/> values.</summary>
public static class SyncPairStateFactory
{
    /// <summary>Creates a state. A negative file count is normalised to zero.</summary>
    public static SyncPairState Create(SyncStatus status, int fileCount, Option<DateTimeOffset> lastSync) => new(status, Math.Max(fileCount, 0), lastSync);
}
