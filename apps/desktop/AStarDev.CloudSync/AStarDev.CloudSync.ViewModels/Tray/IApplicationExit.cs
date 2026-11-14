namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Ends the process once the app has stopped its work (#1409).</summary>
public interface IApplicationExit
{
    /// <summary>Asks the desktop lifetime to shut down.</summary>
    void Shutdown();
}
