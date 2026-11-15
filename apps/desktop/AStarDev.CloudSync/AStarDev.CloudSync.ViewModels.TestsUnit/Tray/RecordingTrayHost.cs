using AStarDev.CloudSync.ViewModels.Tray;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Tray;

internal sealed class RecordingTrayHost : IMainWindowVisibility, IAppShutdown, IApplicationExit
{
    public int Shown { get; private set; }

    public int Hidden { get; private set; }

    public int Minimised { get; private set; }

    public int QuitRequests { get; private set; }

    public int Exits { get; private set; }

    public ShutdownState State => ShutdownStateFactory.CreateRunning();

    public void Show() => Shown++;

    public void Hide() => Hidden++;

    public void Minimise() => Minimised++;

    public Task RequestQuitAsync(CancellationToken cancellationToken)
    {
        QuitRequests++;

        return Task.CompletedTask;
    }

    public void Shutdown() => Exits++;
}
