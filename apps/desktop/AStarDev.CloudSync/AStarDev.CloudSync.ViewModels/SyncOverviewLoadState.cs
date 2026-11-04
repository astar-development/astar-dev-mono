namespace AStarDev.CloudSync.ViewModels;

/// <summary>How far the overview load has got.</summary>
public abstract record SyncOverviewLoadState
{
    private SyncOverviewLoadState()
    {
    }

    /// <summary>The overview has not been read yet.</summary>
    public sealed record NotLoaded : SyncOverviewLoadState;

    /// <summary>The overview was read from the source.</summary>
    public sealed record Loaded : SyncOverviewLoadState;

    /// <summary>The source failed; the failure has been logged.</summary>
    public sealed record Failed : SyncOverviewLoadState;
}
