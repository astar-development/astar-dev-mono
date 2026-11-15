namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>What the user chose when they asked to quit while a sync was running (#1409).</summary>
public abstract record QuitChoice
{
    private QuitChoice()
    {
    }

    /// <summary>"Quit": cancel the running syncs cleanly, then exit.</summary>
    public sealed record QuitAnyway : QuitChoice
    {
        internal QuitAnyway()
        {
        }
    }

    /// <summary>"Keep syncing": the app stays open and the syncs carry on.</summary>
    public sealed record StayOpen : QuitChoice
    {
        internal StayOpen()
        {
        }
    }
}
