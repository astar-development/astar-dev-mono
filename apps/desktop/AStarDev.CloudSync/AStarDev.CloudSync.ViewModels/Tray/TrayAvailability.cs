namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Whether the desktop can show a tray icon (#1409). GNOME, for one, has none without an extension.</summary>
public abstract record TrayAvailability
{
    private TrayAvailability()
    {
    }

    /// <summary>The tray icon can be seen, so closing the window may hide it.</summary>
    public sealed record Available : TrayAvailability
    {
        internal Available()
        {
        }
    }

    /// <summary>No tray icon can be seen, so closing the window must leave it reachable.</summary>
    public sealed record Missing : TrayAvailability
    {
        internal Missing()
        {
        }
    }
}
