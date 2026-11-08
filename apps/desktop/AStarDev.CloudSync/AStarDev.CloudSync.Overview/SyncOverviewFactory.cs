using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Core.Activity;
using AStarDev.CloudSync.Core.Conflicts;
using AStarDev.CloudSync.Core.SyncPairs;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Overview;

/// <summary>Creates <see cref="SyncOverview"/> values.</summary>
public static class SyncOverviewFactory
{
    /// <summary>Creates an overview. Missing lists are normalised to empty lists and negative bytes moved today to zero, and a missing last checked time to none.</summary>
    public static SyncOverview Create(IReadOnlyList<CloudAccount> accounts, IReadOnlyList<SyncPair> syncPairs, IReadOnlyList<ActivityEntry> activity, IReadOnlyList<SyncConflict> conflicts, IReadOnlyList<ThroughputSample> throughput, long bytesMovedToday, Option<DateTimeOffset> lastChecked) => new(accounts ?? [], syncPairs ?? [], activity ?? [], conflicts ?? [], throughput ?? [], Math.Max(bytesMovedToday, 0), lastChecked ?? Option.None<DateTimeOffset>());
}
