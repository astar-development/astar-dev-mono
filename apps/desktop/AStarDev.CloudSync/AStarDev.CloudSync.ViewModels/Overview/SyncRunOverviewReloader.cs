using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Common;

namespace AStarDev.CloudSync.ViewModels.Overview;

/// <summary>Reloads the overview whenever a sync run starts or ends, or its progress is published, so each account's status pill follows its runs (#1402, #1563).</summary>
public sealed class SyncRunOverviewReloader
{
    private readonly SyncOverviewViewModel overview;
    private readonly IUiDispatcher dispatcher;

    /// <summary>Starts following the sync runs.</summary>
    /// <param name="activity">Raises the run started and ended events.</param>
    /// <param name="progress">Raises an event as a run's progress is published.</param>
    /// <param name="overview">The overview to reload.</param>
    /// <param name="dispatcher">Runs the reload on the UI thread.</param>
    public SyncRunOverviewReloader(ISyncActivitySource activity, ISyncProgressFeed progress, SyncOverviewViewModel overview, IUiDispatcher dispatcher)
    {
        this.overview = overview;
        this.dispatcher = dispatcher;
        progress.ProgressChanged += (_, _) => Reload();
        activity.PassStarted += (_, _) => Reload();
        activity.PassFinished += (_, _) => Reload();
    }

    private void Reload() => _ = dispatcher.InvokeAsync(() => _ = overview.LoadAsync(CancellationToken.None));
}
