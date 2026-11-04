namespace AStarDev.CloudSync.Core;

/// <summary>Creates <see cref="SyncOverview"/> values.</summary>
public static class SyncOverviewFactory
{
    /// <summary>Creates an overview. Missing lists are normalised to empty lists.</summary>
    public static SyncOverview Create(IReadOnlyList<CloudAccount> accounts, IReadOnlyList<SyncPair> syncPairs, IReadOnlyList<ActivityEntry> activity, IReadOnlyList<SyncConflict> conflicts, IReadOnlyList<ThroughputSample> throughput) => new(accounts ?? [], syncPairs ?? [], activity ?? [], conflicts ?? [], throughput ?? []);
}
