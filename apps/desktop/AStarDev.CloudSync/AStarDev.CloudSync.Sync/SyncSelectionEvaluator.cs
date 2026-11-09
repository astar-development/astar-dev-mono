namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class SyncSelectionEvaluator : ISyncSelectionEvaluator
{
    /// <inheritdoc />
    public SelectionState Evaluate(LocalRelativePath path, IReadOnlyList<SelectionRule> rules) => SelectionStateFactory.CreateExcluded();

    /// <inheritdoc />
    public bool IsIncluded(LocalRelativePath path, IReadOnlyList<SelectionRule> rules) => false;
}
