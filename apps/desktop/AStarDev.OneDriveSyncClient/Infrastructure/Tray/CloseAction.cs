namespace AStarDev.OneDriveSyncClient.Infrastructure.Tray;

/// <summary>Base type for what the application does when the main window is asked to close.</summary>
public abstract record CloseAction
{
    /// <summary>The window is hidden and the process keeps running in the system tray.</summary>
    public sealed record HideToTray : CloseAction;

    /// <summary>The window closes and the application exits.</summary>
    public sealed record ExitApplication : CloseAction;
}
