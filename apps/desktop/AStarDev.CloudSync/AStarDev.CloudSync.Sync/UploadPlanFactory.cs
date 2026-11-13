namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="UploadPlan"/> values.</summary>
public static class UploadPlanFactory
{
    /// <summary>Creates a plan from steps that are already in order.</summary>
    public static UploadPlan Create(IEnumerable<UploadPlanItem> items) => new([.. items]);
}
