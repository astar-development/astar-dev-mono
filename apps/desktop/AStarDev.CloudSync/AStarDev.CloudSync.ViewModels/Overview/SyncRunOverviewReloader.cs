using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Common;

namespace AStarDev.CloudSync.ViewModels.Overview;

/// <summary>Reloads the overview when a sync run starts or ends, or a status is saved, so every layout shows the account's current status (#1402). Resolve it once at startup.</summary>
public sealed class SyncRunOverviewReloader
{
    /// <summary>Starts listening.</summary>
    /// <param name="activity">Raises the run started and ended events.</param>
    /// <param name="outcomes">Raises the status saved event.</param>
    /// <param name="overview">The overview to reload.</param>
    /// <param name="dispatcher">Runs the reload on the UI thread, because the events are raised off it.</param>
    public SyncRunOverviewReloader(ISyncActivitySource activity, ISyncOutcomeReader outcomes, SyncOverviewViewModel overview, IUiDispatcher dispatcher) => throw new NotImplementedException($"{activity}{outcomes}{overview}{dispatcher}");
}
