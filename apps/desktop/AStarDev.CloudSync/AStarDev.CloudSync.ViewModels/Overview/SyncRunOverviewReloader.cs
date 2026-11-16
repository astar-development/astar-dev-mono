using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Common;

namespace AStarDev.CloudSync.ViewModels.Overview;

/// <summary>Reloads the overview whenever a sync run starts or ends, or its progress is published, so each account's status pill follows its runs (#1402, #1563). A burst of progress events is joined into one reload every two seconds, so the tiles are not rebuilt several times a second while the user clicks (#1796).</summary>
public sealed class SyncRunOverviewReloader
{
    private readonly SyncOverviewViewModel overview;
    private readonly IUiDispatcher dispatcher;
    private static readonly TimeSpan ProgressPause = TimeSpan.FromSeconds(2);

    private readonly TimeProvider timeProvider;
    private int reloadPending;

    /// <summary>Starts following the sync runs.</summary>
    /// <param name="activity">Raises the run started and ended events.</param>
    /// <param name="progress">Raises an event as a run's progress is published.</param>
    /// <param name="overview">The overview to reload.</param>
    /// <param name="dispatcher">Runs the reload on the UI thread.</param>
    /// <param name="timeProvider">Times the pause that joins a burst of progress events into one reload.</param>
    public SyncRunOverviewReloader(ISyncActivitySource activity, ISyncProgressFeed progress, SyncOverviewViewModel overview, IUiDispatcher dispatcher, TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
        this.overview = overview;
        this.dispatcher = dispatcher;
        progress.ProgressChanged += (_, _) => ReloadSoon();
        activity.PassStarted += (_, _) => Reload();
        activity.PassFinished += (_, _) => Reload();
    }

    private void ReloadSoon()
    {
        if (Interlocked.Exchange(ref reloadPending, 1) == 0)
            _ = ReloadAfterPauseAsync();
    }

    private async Task ReloadAfterPauseAsync()
    {
        await Task.Delay(ProgressPause, timeProvider).ConfigureAwait(false);
        Volatile.Write(ref reloadPending, 0);
        Reload();
    }

    private void Reload() => _ = dispatcher.InvokeAsync(() => _ = overview.LoadAsync(CancellationToken.None));
}
