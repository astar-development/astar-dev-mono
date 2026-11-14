using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class ConflictResolutionPlanner : IConflictResolutionPlanner
{
    /// <inheritdoc />
    public Result<ResolutionPlan, ResolutionNotAllowed> Plan(SyncAttentionItem item, ConflictStrategy strategy) =>
        throw new NotImplementedException($"{nameof(Plan)} {item} {strategy}");
}
