using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Overview;

/// <summary>Everything the main window shows: accounts, sync pairs, activity, conflicts, throughput, the data moved today and when sync was last checked.</summary>
public sealed record SyncOverview
{
    internal SyncOverview(
        IReadOnlyList<CloudAccount> accounts,
        IReadOnlyList<SyncPair> syncPairs,
        IReadOnlyList<ActivityEntry> activity,
        IReadOnlyList<SyncConflict> conflicts,
        IReadOnlyList<ThroughputSample> throughput,
        long bytesMovedToday,
        Option<DateTimeOffset> lastChecked)
    {
        Accounts = accounts;
        SyncPairs = syncPairs;
        Activity = activity;
        Conflicts = conflicts;
        Throughput = throughput;
        BytesMovedToday = bytesMovedToday;
        LastChecked = lastChecked;
    }

    /// <summary>The connected accounts.</summary>
    public IReadOnlyList<CloudAccount> Accounts { get; }

    /// <summary>The sync pairs across every account.</summary>
    public IReadOnlyList<SyncPair> SyncPairs { get; }

    /// <summary>Recent activity across every account.</summary>
    public IReadOnlyList<ActivityEntry> Activity { get; }

    /// <summary>Conflicts waiting for the user.</summary>
    public IReadOnlyList<SyncConflict> Conflicts { get; }

    /// <summary>Recent transfer rates, oldest first.</summary>
    public IReadOnlyList<ThroughputSample> Throughput { get; }

    /// <summary>Bytes uploaded and downloaded today across every account.</summary>
    public long BytesMovedToday { get; }

    /// <summary>When sync last checked for changes, if it ever has.</summary>
    public Option<DateTimeOffset> LastChecked { get; }
}
