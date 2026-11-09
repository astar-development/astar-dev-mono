namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SelectionRuleKind"/> cases.</summary>
public static class SelectionRuleKindFactory
{
    /// <summary>Creates the <see cref="SelectionRuleKind.Include"/> case.</summary>
    public static SelectionRuleKind CreateInclude(SyncDirection direction) => new SelectionRuleKind.Include(direction);

    /// <summary>Creates the <see cref="SelectionRuleKind.Exclude"/> case.</summary>
    public static SelectionRuleKind CreateExclude() => new SelectionRuleKind.Exclude();
}
