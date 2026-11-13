namespace AStarDev.CloudSync.Sync;

/// <summary>What the download pass does, in the order it does it, and the local deletions the guard held back (#1400).</summary>
public sealed record DownloadPlan
{
    internal DownloadPlan(IReadOnlyList<DownloadPlanItem> items, IReadOnlyList<HeldLocalDeletions> held)
    {
        Items = items;
        Held = held;
    }

    /// <summary>The steps: folders first (parents before children), then moves, downloads, conflicts, and deletions last (children before parents). One step per path, ignoring case.</summary>
    public IReadOnlyList<DownloadPlanItem> Items { get; }

    /// <summary>Pairs whose local deletions were held by the mass-deletion guard. The other steps of those pairs still run.</summary>
    public IReadOnlyList<HeldLocalDeletions> Held { get; }
}
