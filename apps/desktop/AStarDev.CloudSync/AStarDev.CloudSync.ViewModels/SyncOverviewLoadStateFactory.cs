namespace AStarDev.CloudSync.ViewModels;

/// <summary>Creates <see cref="SyncOverviewLoadState"/> cases.</summary>
public static class SyncOverviewLoadStateFactory
{
    /// <summary>Creates the state before the overview has been read.</summary>
    public static SyncOverviewLoadState CreateNotLoaded() => new SyncOverviewLoadState.NotLoaded();

    /// <summary>Creates the state after the overview was read.</summary>
    public static SyncOverviewLoadState CreateLoaded() => new SyncOverviewLoadState.Loaded();

    /// <summary>Creates the state after the source failed.</summary>
    public static SyncOverviewLoadState CreateFailed() => new SyncOverviewLoadState.Failed();
}
