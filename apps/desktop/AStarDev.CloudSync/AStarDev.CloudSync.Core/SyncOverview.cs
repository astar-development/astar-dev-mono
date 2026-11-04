namespace AStarDev.CloudSync.Core;

/// <summary>Everything the main window shows: accounts, sync pairs, activity, conflicts, throughput and the data moved today.</summary>
/// <param name="Accounts">The connected accounts.</param>
/// <param name="SyncPairs">The sync pairs across every account.</param>
/// <param name="Activity">Recent activity across every account.</param>
/// <param name="Conflicts">Conflicts waiting for the user.</param>
/// <param name="Throughput">Recent transfer rates, oldest first.</param>
/// <param name="BytesMovedToday">Bytes uploaded and downloaded today across every account.</param>
public sealed record SyncOverview(IReadOnlyList<CloudAccount> Accounts, IReadOnlyList<SyncPair> SyncPairs, IReadOnlyList<ActivityEntry> Activity, IReadOnlyList<SyncConflict> Conflicts, IReadOnlyList<ThroughputSample> Throughput, long BytesMovedToday);
