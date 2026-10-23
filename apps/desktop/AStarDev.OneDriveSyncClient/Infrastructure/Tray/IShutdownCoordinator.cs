namespace AStarDev.OneDriveSyncClient.Infrastructure.Tray;

/// <summary>Decides whether closing the main window hides it to the tray or exits the application.</summary>
public interface IShutdownCoordinator
{
    /// <summary>Records that the user explicitly asked to quit, so the next window close exits the application.</summary>
    void RequestQuit();

    /// <summary>Returns the action to take when the main window is asked to close.</summary>
    CloseAction DecideOnWindowClose();
}
