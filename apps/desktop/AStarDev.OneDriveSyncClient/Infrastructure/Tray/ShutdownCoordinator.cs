namespace AStarDev.OneDriveSyncClient.Infrastructure.Tray;

/// <inheritdoc />
public sealed class ShutdownCoordinator : IShutdownCoordinator
{
    /// <inheritdoc />
    public void RequestQuit() => throw new NotImplementedException();

    /// <inheritdoc />
    public CloseAction DecideOnWindowClose() => throw new NotImplementedException();
}
