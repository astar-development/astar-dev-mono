namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Tells whether the desktop can show a tray icon (#1409).</summary>
public interface ITrayAvailability
{
    /// <summary>Checks the desktop. An answer that cannot be found counts as <see cref="TrayAvailability.Missing"/>, so the user is never left with an invisible process.</summary>
    TrayAvailability Check();
}
