namespace AStar.Dev.Velopack.Publishing;

/// <summary>Lets an app hold an update back until it is safe to restart (#1412). A sync client, for example, must not restart in the middle of a sync.</summary>
public interface IUpdateApplyGate
{
    /// <summary>Waits until the app can restart, and keeps it safe until the permit is disposed. The update is applied while the permit is held; if applying fails the permit is disposed so the app carries on.</summary>
    /// <param name="cancellationToken">Stops waiting.</param>
    /// <returns>A permit that holds the app in its safe state.</returns>
    Task<IDisposable> WaitUntilSafeAsync(CancellationToken cancellationToken);
}
