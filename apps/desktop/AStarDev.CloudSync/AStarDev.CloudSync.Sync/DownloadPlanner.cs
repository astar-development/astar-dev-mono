namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="evaluator">Judges paths against the selection rules.</param>
internal sealed class DownloadPlanner(ISyncSelectionEvaluator evaluator) : IDownloadPlanner
{
    /// <inheritdoc />
    public DownloadPlan Plan(DownloadPlanRequest request) => throw new NotImplementedException($"{evaluator}{request}");
}
