using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.DisplaySettings;

public class GivenAnAutostartSettingsViewModel
{
    private sealed class FakeRegistration : IAutostartRegistration
    {
        public AutostartState State { get; set; } = AutostartStateFactory.CreateDisabled();

        public bool ReadFails { get; set; }

        public bool WriteFails { get; set; }

        public List<string> Calls { get; } = [];

        public Task<Result<AutostartState, ErrorResponse>> GetAsync(CancellationToken cancellationToken) => Task.FromResult(ReadFails ? Fail() : Ok(State));

        public Task<Result<AutostartState, ErrorResponse>> EnableAsync(CancellationToken cancellationToken)
        {
            Calls.Add("enable");

            return Task.FromResult(WriteFails ? Fail() : Ok(State = AutostartStateFactory.CreateEnabled()));
        }

        public Task<Result<AutostartState, ErrorResponse>> DisableAsync(CancellationToken cancellationToken)
        {
            Calls.Add("disable");

            return Task.FromResult(WriteFails ? Fail() : Ok(State = AutostartStateFactory.CreateDisabled()));
        }

        private static Result<AutostartState, ErrorResponse> Ok(AutostartState state) => Result.Success<AutostartState, ErrorResponse>(state);

        private static Result<AutostartState, ErrorResponse> Fail() => Result.Failure<AutostartState, ErrorResponse>(new ErrorResponse("locked"));
    }

    private readonly FakeRegistration registration = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string Text(string key) => TestData.CreateLocalization().GetLocal(key);

    private async Task<AutostartSettingsViewModel> LoadedAsync()
    {
        var model = new AutostartSettingsViewModel(registration, TestData.CreateLocalization());
        await model.LoadAsync(CancellationToken);

        return model;
    }

    [Fact]
    public async Task when_it_is_registered_then_the_box_is_ticked_and_can_be_changed()
    {
        registration.State = AutostartStateFactory.CreateEnabled();

        var model = await LoadedAsync();

        (model.IsEnabled, model.CanChange, model.ReasonText, model.ErrorText).ShouldBe((true, true, string.Empty, string.Empty));
    }

    [Fact]
    public async Task when_it_is_not_registered_then_the_box_is_clear_and_can_be_changed()
    {
        var model = await LoadedAsync();

        (model.IsEnabled, model.CanChange).ShouldBe((false, true));
    }

    [Fact]
    public async Task when_the_build_is_not_installed_then_the_box_cannot_be_changed_and_says_why()
    {
        registration.State = AutostartStateFactory.CreateNotSupported(AutostartStateFactory.CreateNotInstalledBuild());

        var model = await LoadedAsync();

        (model.IsEnabled, model.CanChange, model.ToggleCommand.CanExecute(null), model.ReasonText).ShouldBe((false, false, false, Text("Settings.Autostart.NotInstalled")));
    }

    [Fact]
    public async Task when_the_platform_is_not_supported_then_the_box_cannot_be_changed_and_says_why()
    {
        registration.State = AutostartStateFactory.CreateNotSupported(AutostartStateFactory.CreatePlatformNotSupported());

        var model = await LoadedAsync();

        (model.CanChange, model.ReasonText).ShouldBe((false, Text("Settings.Autostart.PlatformNotSupported")));
    }

    [Fact]
    public async Task when_the_box_is_toggled_on_then_it_registers_and_is_ticked_and_toggled_off_then_it_unregisters()
    {
        var model = await LoadedAsync();

        await model.ToggleCommand.ExecuteAsync(null);
        var afterOn = model.IsEnabled;
        await model.ToggleCommand.ExecuteAsync(null);

        (afterOn, model.IsEnabled).ShouldBe((true, false));
        registration.Calls.ShouldBe(["enable", "disable"]);
    }

    [Fact]
    public async Task when_registering_fails_then_the_box_stays_clear_and_a_message_says_so()
    {
        var model = await LoadedAsync();
        registration.WriteFails = true;

        await model.ToggleCommand.ExecuteAsync(null);

        (model.IsEnabled, model.ErrorText).ShouldBe((false, Text("Settings.Autostart.ChangeFailed")));
    }

    [Fact]
    public async Task when_reading_the_state_fails_then_the_box_cannot_be_changed_and_a_message_says_so()
    {
        registration.ReadFails = true;

        var model = await LoadedAsync();

        (model.CanChange, model.ErrorText).ShouldBe((false, Text("Settings.Autostart.LoadFailed")));
    }

    [Fact]
    public async Task when_the_texts_are_read_then_they_are_the_heading_and_the_label()
    {
        var model = await LoadedAsync();

        (model.HeadingText, model.ToggleText).ShouldBe((Text("Settings.Autostart"), Text("Settings.Autostart.Toggle")));
    }
}
