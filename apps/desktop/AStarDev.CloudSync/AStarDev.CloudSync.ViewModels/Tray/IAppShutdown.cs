namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Quits the app on purpose: running passes are cancelled cleanly first (#1409).</summary>
public interface IAppShutdown
{
    /// <summary>Whether a quit was requested.</summary>
    ShutdownState State { get; }

    /// <summary>Cancels every running pass, waits for it to stop and then exits. A second request does nothing.</summary>
    /// <param name="cancellationToken">Stops waiting for the passes.</param>
    Task RequestQuitAsync(CancellationToken cancellationToken);
}
