using AStarDev.OneDriveSyncClient.Infrastructure.Tray;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Tray;

public sealed class GivenAShutdownCoordinator
{
    [Fact]
    public void when_quit_has_not_been_requested_then_window_close_hides_to_tray()
    {
        var sut = new ShutdownCoordinator();

        var action = sut.DecideOnWindowClose();

        action.ShouldBeOfType<CloseAction.HideToTray>();
    }

    [Fact]
    public void when_quit_has_been_requested_then_window_close_exits()
    {
        var sut = new ShutdownCoordinator();
        sut.RequestQuit();

        var action = sut.DecideOnWindowClose();

        action.ShouldBeOfType<CloseAction.ExitApplication>();
    }

    [Fact]
    public void when_window_is_closed_repeatedly_without_quit_then_it_keeps_hiding_to_tray()
    {
        var sut = new ShutdownCoordinator();
        _ = sut.DecideOnWindowClose();

        var action = sut.DecideOnWindowClose();

        action.ShouldBeOfType<CloseAction.HideToTray>();
    }
}
