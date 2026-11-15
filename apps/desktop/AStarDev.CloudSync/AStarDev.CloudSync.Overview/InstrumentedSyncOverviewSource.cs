using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Overview;

/// <summary>Counts and times every overview refresh (#1518).</summary>
/// <param name="inner">Builds the overview.</param>
/// <param name="telemetry">Records the refresh.</param>
internal sealed class InstrumentedSyncOverviewSource(ThrottleHintingSyncOverviewSource inner, IWorkflowTelemetry telemetry) : ISyncOverviewSource
{
    /// <inheritdoc />
    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) =>
        telemetry.TrackAsync(TelemetryWorkflowFactory.CreateOverviewRefresh(), () => inner.GetOverviewAsync(cancellationToken), result => result.Match(_ => TelemetryOutcomeFactory.CreateSucceeded(), _ => TelemetryOutcomeFactory.CreateFailed()));
}
