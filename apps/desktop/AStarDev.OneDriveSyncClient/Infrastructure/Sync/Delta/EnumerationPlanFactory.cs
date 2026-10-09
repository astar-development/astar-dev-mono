namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;

/// <summary>Creates <see cref="EnumerationPlan"/> cases.</summary>
public static class EnumerationPlanFactory
{
    /// <summary>Creates a <see cref="WalkRequired"/> case.</summary>
    public static EnumerationPlan CreateWalkRequired(string reason) => new WalkRequired(reason);

    /// <summary>Creates a <see cref="CheckForChanges"/> case.</summary>
    public static EnumerationPlan CreateCheckForChanges(string deltaLink) => new CheckForChanges(deltaLink);
}
