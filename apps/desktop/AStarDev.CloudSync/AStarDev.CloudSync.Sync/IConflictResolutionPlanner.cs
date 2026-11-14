using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Decides what a strategy does to an attention item (#1406). Pure: it reads nothing and writes nothing.</summary>
public interface IConflictResolutionPlanner
{
    /// <summary>Plans the resolution of one item.</summary>
    /// <param name="item">The queue item.</param>
    /// <param name="strategy">The strategy the user chose.</param>
    /// <returns>The plan, or why the strategy does not apply to the item.</returns>
    Result<ResolutionPlan, ResolutionNotAllowed> Plan(SyncAttentionItem item, ConflictStrategy strategy);
}
