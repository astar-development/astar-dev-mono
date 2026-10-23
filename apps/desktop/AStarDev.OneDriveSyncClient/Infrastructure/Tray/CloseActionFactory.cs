namespace AStarDev.OneDriveSyncClient.Infrastructure.Tray;

/// <summary>Creates <see cref="CloseAction"/> instances for each close case.</summary>
public static class CloseActionFactory
{
    /// <summary>Creates the action that hides the window to the system tray.</summary>
    public static CloseAction CreateHideToTray() => new CloseAction.HideToTray();

    /// <summary>Creates the action that closes the window and exits the application.</summary>
    public static CloseAction CreateExitApplication() => new CloseAction.ExitApplication();
}
