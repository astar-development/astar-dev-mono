using AStar.Dev.Velopack.Publishing;
using AStarDev.OneDriveSyncClient.Infrastructure.Autostart;
using AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Autostart;

public sealed class GivenAnAutostartService
{
    private const string LinuxEntryPath = "/home/test/.config/autostart/astar-onedrive-sync.desktop";
    private const string MacAgentPath = "/Users/test/Library/LaunchAgents/com.astar.onedrive-sync.plist";

    private readonly MockFileSystem fileSystem = new();
    private readonly IAutostartContextProvider contextProvider = Substitute.For<IAutostartContextProvider>();
    private readonly IVelopackUpdateService velopack = Substitute.For<IVelopackUpdateService>();
    private readonly IWindowsRunKey windowsRunKey = Substitute.For<IWindowsRunKey>();

    public GivenAnAutostartService() => _ = velopack.IsInstalled.Returns(true);

    private AutostartService CreateSut() => new(fileSystem, contextProvider, velopack, windowsRunKey);

    private void UseContext(DesktopPlatform platform, string home, string xdg = "") => _ = contextProvider.Current().Returns(AutostartContextFactory.Create(platform, "/opt/astar/app", home, xdg));

    [Fact]
    public void when_the_app_is_not_installed_then_nothing_is_registered()
    {
        _ = velopack.IsInstalled.Returns(false);
        UseContext(DesktopPlatformFactory.CreateLinux(), "/home/test");

        var outcome = CreateSut().EnsureRegistered();

        outcome.ShouldBeOfType<AutostartOutcome.NotInstalled>();
        fileSystem.File.Exists(LinuxEntryPath).ShouldBeFalse();
        windowsRunKey.DidNotReceive().SetValue(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_installed_on_linux_then_a_desktop_entry_is_written_to_the_autostart_directory()
    {
        UseContext(DesktopPlatformFactory.CreateLinux(), "/home/test");

        var outcome = CreateSut().EnsureRegistered();

        outcome.ShouldBe(AutostartOutcomeFactory.CreateRegistered(LinuxEntryPath));
        fileSystem.File.ReadAllText(LinuxEntryPath).ShouldBe(AutostartEntryBuilder.BuildDesktopEntry("/opt/astar/app", 30));
    }

    [Fact]
    public void when_xdg_config_home_is_set_on_linux_then_the_desktop_entry_is_written_beneath_it()
    {
        UseContext(DesktopPlatformFactory.CreateLinux(), "/home/test", "/custom/config");

        var outcome = CreateSut().EnsureRegistered();

        outcome.ShouldBe(AutostartOutcomeFactory.CreateRegistered("/custom/config/autostart/astar-onedrive-sync.desktop"));
    }

    [Fact]
    public void when_a_stale_desktop_entry_exists_then_it_is_overwritten()
    {
        UseContext(DesktopPlatformFactory.CreateLinux(), "/home/test");
        _ = fileSystem.Directory.CreateDirectory("/home/test/.config/autostart");
        fileSystem.File.WriteAllText(LinuxEntryPath, "stale");

        _ = CreateSut().EnsureRegistered();

        fileSystem.File.ReadAllText(LinuxEntryPath).ShouldBe(AutostartEntryBuilder.BuildDesktopEntry("/opt/astar/app", 30));
    }

    [Fact]
    public void when_installed_on_macos_then_a_launch_agent_is_written()
    {
        UseContext(DesktopPlatformFactory.CreateMacOs(), "/Users/test");

        var outcome = CreateSut().EnsureRegistered();

        outcome.ShouldBe(AutostartOutcomeFactory.CreateRegistered(MacAgentPath));
        fileSystem.File.ReadAllText(MacAgentPath).ShouldBe(AutostartEntryBuilder.BuildLaunchAgent("com.astar.onedrive-sync", "/opt/astar/app"));
    }

    [Fact]
    public void when_installed_on_windows_then_the_run_key_value_is_set()
    {
        UseContext(DesktopPlatformFactory.CreateWindows(), "C:\\Users\\test");

        var outcome = CreateSut().EnsureRegistered();

        outcome.ShouldBe(AutostartOutcomeFactory.CreateRegistered("AStarOneDriveSync"));
        windowsRunKey.Received(1).SetValue("AStarOneDriveSync", AutostartEntryBuilder.BuildWindowsCommand("/opt/astar/app"));
    }

    [Fact]
    public void when_the_windows_run_key_cannot_be_written_then_a_failed_outcome_is_returned()
    {
        UseContext(DesktopPlatformFactory.CreateWindows(), "C:\\Users\\test");
        windowsRunKey.When(key => key.SetValue(Arg.Any<string>(), Arg.Any<string>())).Throw(new UnauthorizedAccessException("denied"));

        var outcome = CreateSut().EnsureRegistered();

        outcome.ShouldBe(AutostartOutcomeFactory.CreateFailed("denied"));
    }

    [Fact]
    public void when_the_home_directory_is_unknown_then_a_failed_outcome_is_returned_and_nothing_is_written()
    {
        UseContext(DesktopPlatformFactory.CreateLinux(), "");

        var outcome = CreateSut().EnsureRegistered();

        outcome.ShouldBeOfType<AutostartOutcome.Failed>();
        fileSystem.Directory.GetFiles("/", "*", SearchOption.AllDirectories).ShouldBeEmpty();
    }
}
