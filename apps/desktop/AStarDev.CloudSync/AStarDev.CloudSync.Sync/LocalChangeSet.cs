namespace AStarDev.CloudSync.Sync;

/// <summary>The result of one local scan: every change since the baseline, sorted by path ignoring case, with a summary (#1399).</summary>
public sealed record LocalChangeSet
{
    internal LocalChangeSet(IReadOnlyList<LocalChange> changes, IReadOnlyList<MassDeletionSuspected> holds, LocalScanSummary summary)
    {
        Changes = changes;
        Holds = holds;
        Summary = summary;
    }

    /// <summary>The changes, each path at most once.</summary>
    public IReadOnlyList<LocalChange> Changes { get; }

    /// <summary>The pairs whose deletions the mass-deletion guard held, one entry per pair. Their deleted paths are not in <see cref="Changes"/>.</summary>
    public IReadOnlyList<MassDeletionSuspected> Holds { get; }

    /// <summary>The counts.</summary>
    public LocalScanSummary Summary { get; }
}
