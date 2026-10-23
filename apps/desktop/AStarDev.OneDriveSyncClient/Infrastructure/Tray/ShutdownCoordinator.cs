namespace AStarDev.OneDriveSyncClient.Infrastructure.Tray;

/// <inheritdoc />
public sealed class ShutdownCoordinator : IShutdownCoordinator
{
    private volatile CloseAction pendingAction = CloseActionFactory.CreateHideToTray();

    /// <inheritdoc />
    public void RequestQuit() => pendingAction = CloseActionFactory.CreateExitApplication();

    /// <inheritdoc />
    public CloseAction DecideOnWindowClose() => pendingAction;
}
