using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="evaluator">Tells which paths are in an upload pair.</param>
internal sealed class UploadPlanner(ISyncSelectionEvaluator evaluator) : IUploadPlanner
{
    /// <inheritdoc />
    public UploadPlan Plan(LocalChangeSet changes, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> baseline, IReadOnlyDictionary<RelativeSyncPath, RemoteItemSnapshot> remote, IReadOnlyList<SelectionRule> rules, IRemoteNamePolicy names) =>
        throw new NotImplementedException($"{evaluator}{changes}{baseline}{remote}{rules}{names}");
}
