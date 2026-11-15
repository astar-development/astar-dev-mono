using AStarDev.CloudSync.Autostart;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenAWindowsAutostartRegistration
{
    private const string ValueName = "AStarDevCloudSync";

    private readonly FakeRunKeyStore runKey = new();

    private sealed class FakeRunKeyStore : IRunKeyStore
    {
        public Dictionary<string, string> Values { get; } = [];

        public bool Fails { get; set; }

        public Option<string> Read(string name) => Fails ? throw new UnauthorizedAccessException("denied") : Values.TryGetValue(name, out var command) ? Option.Some(command) : Option.None<string>();

        public void Write(string name, string command)
        {
            if (Fails)
                throw new UnauthorizedAccessException("denied");

            Values[name] = command;
        }

        public void Remove(string name)
        {
            if (Fails)
                throw new UnauthorizedAccessException("denied");

            _ = Values.Remove(name);
        }
    }

    private sealed class Launcher(Option<string> path) : IAutostartLauncher
    {
        public Option<string> LauncherPath => path;
    }

    private WindowsAutostartRegistration Create(Option<string>? launcher = null) =>
        new(runKey, new Launcher(launcher ?? Option.Some(@"C:\Users\test\AppData\Local\AStarDevCloudSync\current\CloudSync.exe")), NullLogger<WindowsAutostartRegistration>.Instance);

    private static AutostartState Of(Result<AutostartState, ErrorResponse> result) => result.Match(state => state, error => throw new InvalidOperationException(error.Message));

    [Fact]
    public async Task when_nothing_is_registered_then_the_state_is_disabled()
    {
        Of(await Create().GetAsync(TestContext.Current.CancellationToken)).ShouldBe(AutostartStateFactory.CreateDisabled());
    }

    [Fact]
    public async Task when_it_is_enabled_then_a_value_that_starts_the_app_minimised_is_written_and_the_state_is_enabled()
    {
        var state = Of(await Create().EnableAsync(TestContext.Current.CancellationToken));

        state.ShouldBe(AutostartStateFactory.CreateEnabled());
        runKey.Values[ValueName].ShouldBe("\"C:\\Users\\test\\AppData\\Local\\AStarDevCloudSync\\current\\CloudSync.exe\" --minimized");
    }

    [Fact]
    public async Task when_the_value_exists_then_the_state_is_enabled()
    {
        var registration = Create();
        _ = await registration.EnableAsync(TestContext.Current.CancellationToken);

        Of(await registration.GetAsync(TestContext.Current.CancellationToken)).ShouldBe(AutostartStateFactory.CreateEnabled());
    }

    [Fact]
    public async Task when_it_is_disabled_then_the_value_is_removed_and_disabling_again_is_not_an_error()
    {
        var registration = Create();
        _ = await registration.EnableAsync(TestContext.Current.CancellationToken);

        var first = Of(await registration.DisableAsync(TestContext.Current.CancellationToken));
        var second = Of(await registration.DisableAsync(TestContext.Current.CancellationToken));

        (first, second, runKey.Values.ContainsKey(ValueName)).ShouldBe((AutostartStateFactory.CreateDisabled(), AutostartStateFactory.CreateDisabled(), false));
    }

    [Fact]
    public async Task when_the_build_is_not_installed_then_nothing_is_written_and_the_state_says_why()
    {
        var state = Of(await Create(Option.None<string>()).EnableAsync(TestContext.Current.CancellationToken));

        (state, runKey.Values.Count).ShouldBe((AutostartStateFactory.CreateNotSupported(AutostartStateFactory.CreateNotInstalledBuild()), 0));
    }

    [Fact]
    public async Task when_the_build_is_not_installed_then_reading_the_state_is_not_supported_too()
    {
        Of(await Create(Option.None<string>()).GetAsync(TestContext.Current.CancellationToken)).ShouldBe(AutostartStateFactory.CreateNotSupported(AutostartStateFactory.CreateNotInstalledBuild()));
    }

    [Fact]
    public async Task when_the_key_cannot_be_written_then_enabling_fails_with_an_error_and_not_an_exception()
    {
        runKey.Fails = true;

        var result = await Create().EnableAsync(TestContext.Current.CancellationToken);

        result.Match(_ => "ok", error => error.Message).ShouldBe("The start at login value could not be written.");
    }

    [Fact]
    public async Task when_the_key_cannot_be_read_then_the_state_check_fails_with_an_error()
    {
        runKey.Fails = true;

        var result = await Create().GetAsync(TestContext.Current.CancellationToken);

        result.Match(_ => "ok", error => error.Message).ShouldBe("The start at login value could not be read.");
    }

    [Fact]
    public async Task when_the_key_cannot_be_changed_then_disabling_fails_with_an_error()
    {
        runKey.Fails = true;

        var result = await Create().DisableAsync(TestContext.Current.CancellationToken);

        result.Match(_ => "ok", error => error.Message).ShouldBe("The start at login value could not be removed.");
    }
}
