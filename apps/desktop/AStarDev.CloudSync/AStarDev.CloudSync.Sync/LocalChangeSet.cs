namespace AStarDev.CloudSync.Sync;

/// <summary>The result of one local scan: every change since the baseline, sorted by path ignoring case, with a summary (#1399).</summary>
public sealed record LocalChangeSet
{
    internal LocalChangeSet(IReadOnlyList<LocalChange> changes, LocalScanSummary summary)
    {
        Changes = changes;
        Summary = summary;
    }

    /// <summary>The changes, each path at most once.</summary>
    public IReadOnlyList<LocalChange> Changes { get; }

    /// <summary>The counts.</summary>
    public LocalScanSummary Summary { get; }
}
