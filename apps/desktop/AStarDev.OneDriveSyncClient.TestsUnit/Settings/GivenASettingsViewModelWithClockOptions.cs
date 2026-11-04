using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.ClockControls;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Autostart;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Theme;
using AStarDev.Localization;
using AStarDev.OneDriveSyncClient.Settings;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Settings;

public sealed class GivenASettingsViewModelWithClockOptions
{
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly ILocalizationService _loc = Substitute.For<ILocalizationService>();
    private readonly AppSettings _appSettings = new();

    public GivenASettingsViewModelWithClockOptions()
    {
        _settingsService.Current.Returns(_appSettings);
        _settingsService.SaveAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _loc.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        _loc.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());
    }

    private SettingsViewModel CreateSut() => new(_settingsService, Substitute.For<IThemeService>(), Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), _loc, Substitute.For<IFolderPickerService>(), Substitute.For<IAutostartService>(), Substitute.For<IUpdateNotificationService>());

    [Fact]
    public void when_settings_are_new_then_the_clock_defaults_to_analogue_with_seconds()
    {
        var settings = new AppSettings();

        settings.ClockStyle.ShouldBe("Analogue");
        settings.ShowClockSeconds.ShouldBeTrue();
    }

    [Fact]
    public void when_the_view_model_is_created_then_it_reflects_the_saved_clock_settings()
    {
        _appSettings.ClockStyle = "Digital";
        _appSettings.ShowClockSeconds = false;

        var sut = CreateSut();

        sut.ClockStyle.ShouldBeOfType<DigitalClockStyle>();
        sut.ShowClockSeconds.ShouldBeFalse();
    }

    [Fact]
    public void when_the_clock_style_is_changed_then_it_is_saved_as_a_key()
    {
        var sut = CreateSut();

        sut.ClockStyle = ClockStyleFactory.Digital();

        _appSettings.ClockStyle.ShouldBe("Digital");
        _settingsService.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_show_clock_seconds_is_changed_then_it_is_saved()
    {
        var sut = CreateSut();

        sut.ShowClockSeconds = false;

        _appSettings.ShowClockSeconds.ShouldBeFalse();
        _settingsService.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_the_clock_style_options_are_requested_then_the_current_style_is_selected()
    {
        _appSettings.ClockStyle = "Digital";

        var sut = CreateSut();

        sut.ClockStyleOptions.Single(option => option.IsSelected).Style.ShouldBeOfType<DigitalClockStyle>();
        sut.ClockStyleOptions.Count.ShouldBe(2);
    }

    [Fact]
    public void when_the_clock_style_is_changed_then_the_selected_option_changes()
    {
        var sut = CreateSut();

        sut.ClockStyle = ClockStyleFactory.Digital();

        sut.ClockStyleOptions.Single(option => option.IsSelected).Style.ShouldBeOfType<DigitalClockStyle>();
    }

    [Fact]
    public void when_the_clock_labels_are_requested_then_the_clock_localisation_keys_are_used()
    {
        var sut = CreateSut();

        sut.ClockHeadingText.ShouldBe("Settings.Section.Clock");
        sut.ClockStyleLabelText.ShouldBe("Settings.Clock.Style");
        sut.ClockStyleDescriptionText.ShouldBe("Settings.Clock.Style.Description");
        sut.ShowClockSecondsLabelText.ShouldBe("Settings.Clock.ShowSeconds");
        sut.ShowClockSecondsDescriptionText.ShouldBe("Settings.Clock.ShowSeconds.Description");
        sut.ClockStyleOptions.Select(option => option.Label).ShouldBe(["Settings.Clock.Analogue", "Settings.Clock.Digital"]);
    }

    [Fact]
    public void when_the_settings_service_reports_a_change_then_the_clock_settings_are_refreshed_without_saving()
    {
        var sut = CreateSut();
        _appSettings.ClockStyle = "Digital";
        _appSettings.ShowClockSeconds = false;

        _settingsService.SettingsChanged += Raise.Event<EventHandler<AppSettings>>(_settingsService, _appSettings);

        sut.ClockStyle.ShouldBeOfType<DigitalClockStyle>();
        sut.ShowClockSeconds.ShouldBeFalse();
        _settingsService.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }
}
