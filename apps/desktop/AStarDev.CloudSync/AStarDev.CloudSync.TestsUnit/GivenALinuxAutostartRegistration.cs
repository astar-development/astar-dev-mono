using AStarDev.CloudSync.Autostart;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenALinuxAutostartRegistration
{
    private const string EntryPath = "/home/test/.config/autostart/astardev-cloudsync.desktop";

    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));

    private sealed class Home(MockFileSystem fileSystem, bool present) : IHomeFolder
    {
        public Option<System.IO.Abstractions.IDirectoryInfo> Folder => present ? Option.Some(fileSystem.DirectoryInfo.New("/home/test")) : Option.None<System.IO.Abstractions.IDirectoryInfo>();
    }

    private sealed class Launcher(Option<string> path) : IAutostartLauncher
    {
        public Option<string> LauncherPath => path;
    }

    private LinuxAutostartRegistration Create(Option<string>? launcher = null, bool home = true) =>
        new(fileSystem, new Home(fileSystem, home), new Launcher(launcher ?? Option.Some("/opt/cloudsync/CloudSync")), NullLogger<LinuxAutostartRegistration>.Instance);

    private static AutostartState Of(Result<AutostartState, ErrorResponse> result) => result.Match(state => state, error => throw new InvalidOperationException(error.Message));

    [Fact]
    public async Task when_nothing_is_registered_then_the_state_is_disabled()
    {
        Of(await Create().GetAsync(TestContext.Current.CancellationToken)).ShouldBe(AutostartStateFactory.CreateDisabled());
    }

    [Fact]
    public async Task when_it_is_enabled_then_an_entry_that_starts_the_app_minimised_with_a_delay_is_written_and_the_state_is_enabled()
    {
        var state = Of(await Create().EnableAsync(TestContext.Current.CancellationToken));

        var entry = fileSystem.File.ReadAllText(EntryPath);
        state.ShouldBe(AutostartStateFactory.CreateEnabled());
        entry.ShouldContain("[Desktop Entry]");
        entry.ShouldContain("Type=Application");
        entry.ShouldContain("Exec=\"/opt/cloudsync/CloudSync\" --minimized");
        entry.ShouldContain("X-GNOME-Autostart-Delay=30");
    }

    [Fact]
    public async Task when_the_entry_exists_then_the_state_is_enabled()
    {
        var registration = Create();
        _ = await registration.EnableAsync(TestContext.Current.CancellationToken);

        Of(await registration.GetAsync(TestContext.Current.CancellationToken)).ShouldBe(AutostartStateFactory.CreateEnabled());
    }

    [Fact]
    public async Task when_it_is_disabled_then_the_entry_is_removed_and_disabling_again_is_not_an_error()
    {
        var registration = Create();
        _ = await registration.EnableAsync(TestContext.Current.CancellationToken);

        var first = Of(await registration.DisableAsync(TestContext.Current.CancellationToken));
        var second = Of(await registration.DisableAsync(TestContext.Current.CancellationToken));

        (first, second, fileSystem.File.Exists(EntryPath)).ShouldBe((AutostartStateFactory.CreateDisabled(), AutostartStateFactory.CreateDisabled(), false));
    }

    [Fact]
    public async Task when_the_build_is_not_installed_then_nothing_is_written_and_the_state_says_why()
    {
        var registration = Create(Option.None<string>());

        var state = Of(await registration.EnableAsync(TestContext.Current.CancellationToken));

        (state, fileSystem.File.Exists(EntryPath)).ShouldBe((AutostartStateFactory.CreateNotSupported(AutostartStateFactory.CreateNotInstalledBuild()), false));
    }

    [Fact]
    public async Task when_the_build_is_not_installed_then_reading_the_state_is_not_supported_too()
    {
        Of(await Create(Option.None<string>()).GetAsync(TestContext.Current.CancellationToken)).ShouldBe(AutostartStateFactory.CreateNotSupported(AutostartStateFactory.CreateNotInstalledBuild()));
    }

    [Fact]
    public async Task when_there_is_no_home_folder_then_enabling_fails_with_an_error_and_writes_nothing()
    {
        var result = await Create(home: false).EnableAsync(TestContext.Current.CancellationToken);

        (result.Match(_ => "ok", _ => "failed"), fileSystem.File.Exists(EntryPath)).ShouldBe(("failed", false));
    }

    [Fact]
    public async Task when_the_entry_cannot_be_written_then_enabling_fails_with_an_error_and_not_an_exception()
    {
        _ = fileSystem.Directory.CreateDirectory("/home/test/.config");
        fileSystem.File.WriteAllText("/home/test/.config/autostart", "a file where the folder should be");

        var result = await Create().EnableAsync(TestContext.Current.CancellationToken);

        result.Match(_ => "ok", _ => "failed").ShouldBe("failed");
    }

    [Fact]
    public async Task when_an_enabled_entry_holds_an_old_path_then_reading_the_state_rewrites_it_with_the_current_path()
    {
        _ = await Create(Option.Some("/tmp/.mount_CloudXYZ/usr/bin/CloudSync")).EnableAsync(TestContext.Current.CancellationToken);

        var state = Of(await Create(Option.Some("/home/test/Apps/CloudSync.AppImage")).GetAsync(TestContext.Current.CancellationToken));

        var entry = fileSystem.File.ReadAllText(EntryPath);
        state.ShouldBe(AutostartStateFactory.CreateEnabled());
        entry.ShouldContain("Exec=\"/home/test/Apps/CloudSync.AppImage\" --minimized");
        entry.ShouldNotContain(".mount_");
    }

    [Fact]
    public async Task when_the_build_is_not_installed_then_reading_the_state_leaves_an_existing_entry_alone()
    {
        _ = await Create(Option.Some("/tmp/.mount_CloudXYZ/usr/bin/CloudSync")).EnableAsync(TestContext.Current.CancellationToken);

        _ = await Create(Option.None<string>()).GetAsync(TestContext.Current.CancellationToken);

        fileSystem.File.ReadAllText(EntryPath).ShouldContain(".mount_");
    }
}
