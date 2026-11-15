using AStarDev.CloudSync.Autostart;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenAMacAutostartRegistration
{
    private const string EntryPath = "/Users/test/Library/LaunchAgents/dev.astar.cloudsync.plist";

    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.MacOS));

    private sealed class Home(MockFileSystem fileSystem, bool present) : IHomeFolder
    {
        public Option<System.IO.Abstractions.IDirectoryInfo> Folder => present ? Option.Some(fileSystem.DirectoryInfo.New("/Users/test")) : Option.None<System.IO.Abstractions.IDirectoryInfo>();
    }

    private sealed class Launcher(Option<string> path) : IAutostartLauncher
    {
        public Option<string> LauncherPath => path;
    }

    private MacAutostartRegistration Create(Option<string>? launcher = null, bool home = true) =>
        new(fileSystem, new Home(fileSystem, home), new Launcher(launcher ?? Option.Some("/Applications/CloudSync.app/Contents/MacOS/CloudSync")), NullLogger<MacAutostartRegistration>.Instance);

    private static AutostartState Of(Result<AutostartState, ErrorResponse> result) => result.Match(state => state, error => throw new InvalidOperationException(error.Message));

    [Fact]
    public async Task when_nothing_is_registered_then_the_state_is_disabled()
    {
        Of(await Create().GetAsync(TestContext.Current.CancellationToken)).ShouldBe(AutostartStateFactory.CreateDisabled());
    }

    [Fact]
    public async Task when_it_is_enabled_then_a_plist_that_starts_the_app_minimised_is_written_and_the_state_is_enabled()
    {
        var state = Of(await Create().EnableAsync(TestContext.Current.CancellationToken));

        state.ShouldBe(AutostartStateFactory.CreateEnabled());
        fileSystem.File.ReadAllText(EntryPath).ShouldBe(LaunchAgentPlistBuilder.Build("/Applications/CloudSync.app/Contents/MacOS/CloudSync"));
    }

    [Fact]
    public async Task when_it_is_disabled_then_the_plist_is_removed_and_disabling_again_is_not_an_error()
    {
        var registration = Create();
        _ = await registration.EnableAsync(TestContext.Current.CancellationToken);

        var first = Of(await registration.DisableAsync(TestContext.Current.CancellationToken));
        var second = Of(await registration.DisableAsync(TestContext.Current.CancellationToken));

        (first, second, fileSystem.File.Exists(EntryPath)).ShouldBe((AutostartStateFactory.CreateDisabled(), AutostartStateFactory.CreateDisabled(), false));
    }
}
