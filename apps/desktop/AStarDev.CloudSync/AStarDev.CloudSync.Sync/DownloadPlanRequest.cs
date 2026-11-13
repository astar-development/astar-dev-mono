namespace AStarDev.CloudSync.Sync;

/// <summary>Everything the download planner reads for one account (#1400). The remote index must be complete (<see cref="IRemoteIndexReader.IsCompleteAsync"/>), because an item missing from <see cref="Remote"/> counts as deleted remotely.</summary>
public sealed record DownloadPlanRequest
{
    internal DownloadPlanRequest(IReadOnlyList<RemoteIndexEntry> remote, IReadOnlyList<SyncBaselineEntry> baseline, LocalChangeSet local, IReadOnlyList<SelectionRule> rules, QuarantineSnapshot quarantine, IReadOnlyList<RelativeSyncPath> missingLocally)
    {
        Remote = remote;
        Baseline = baseline;
        Local = local;
        Rules = rules;
        Quarantine = quarantine;
        MissingLocally = missingLocally;
    }

    /// <summary>Every live remote index item with its path. Deleted items are left out.</summary>
    public IReadOnlyList<RemoteIndexEntry> Remote { get; }

    /// <summary>The account's sync baseline.</summary>
    public IReadOnlyList<SyncBaselineEntry> Baseline { get; }

    /// <summary>What the local scan found (#1399).</summary>
    public LocalChangeSet Local { get; }

    /// <summary>The account's selection rules.</summary>
    public IReadOnlyList<SelectionRule> Rules { get; }

    /// <summary>The quarantined paths at the start of the pass.</summary>
    public QuarantineSnapshot Quarantine { get; }

    /// <summary>Baseline files the pass found missing on disk although the scan did not report them as deleted. They are repaired when their remote item is unchanged and their path is clear.</summary>
    public IReadOnlyList<RelativeSyncPath> MissingLocally { get; }
}
