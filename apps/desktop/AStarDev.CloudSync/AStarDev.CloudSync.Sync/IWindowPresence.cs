namespace AStarDev.CloudSync.Sync;

/// <summary>Tells whether the main window is in front (#1410).</summary>
public interface IWindowPresence
{
    /// <summary>The window's state now.</summary>
    WindowPresence Current { get; }
}
