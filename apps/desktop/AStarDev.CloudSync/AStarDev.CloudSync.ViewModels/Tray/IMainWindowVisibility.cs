namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Shows and hides the main window while the app keeps running in the tray (#1409).</summary>
public interface IMainWindowVisibility
{
    /// <summary>Shows the window, restores it if it was minimised and gives it focus.</summary>
    void Show();

    /// <summary>Hides the window. The app and its scheduled syncs keep running.</summary>
    void Hide();

    /// <summary>Minimises the window to the task bar. It is used when there is no tray icon to come back from.</summary>
    void Minimise();
}
