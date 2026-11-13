namespace AStarDev.CloudSync.Sync;

/// <summary>The ordered steps of one upload pass: folders first (parents before children), then file uploads, then deletions (children before parents), then the items that are only reported.</summary>
public sealed record UploadPlan
{
    internal UploadPlan(IReadOnlyList<UploadPlanItem> items)
    {
        Items = items;
    }

    /// <summary>The steps, in the order they must run. There is exactly one step per path (ignoring case).</summary>
    public IReadOnlyList<UploadPlanItem> Items { get; }
}
