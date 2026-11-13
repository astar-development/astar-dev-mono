namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DownloadPlan"/> values.</summary>
public static class DownloadPlanFactory
{
    /// <summary>Creates a plan. Missing collections read as empty.</summary>
    /// <param name="items">The steps, already in execution order.</param>
    /// <param name="held">The held deletions.</param>
    /// <returns>The plan.</returns>
    public static DownloadPlan Create(IEnumerable<DownloadPlanItem> items, IEnumerable<HeldLocalDeletions> held) => new([.. items ?? []], [.. held ?? []]);
}
