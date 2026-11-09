namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SelectionState"/> cases.</summary>
public static class SelectionStateFactory
{
    /// <summary>Creates the <see cref="SelectionState.Included"/> case.</summary>
    public static SelectionState CreateIncluded(SyncPairId pair, SyncDirection direction) => new SelectionState.Included(pair, direction);

    /// <summary>Creates the <see cref="SelectionState.Excluded"/> case.</summary>
    public static SelectionState CreateExcluded() => new SelectionState.Excluded();

    /// <summary>Creates the <see cref="SelectionState.Uncovered"/> case.</summary>
    public static SelectionState CreateUncovered() => new SelectionState.Uncovered();

    /// <summary>Creates the <see cref="SelectionState.PartiallyIncluded"/> case.</summary>
    public static SelectionState CreatePartiallyIncluded() => new SelectionState.PartiallyIncluded();
}
