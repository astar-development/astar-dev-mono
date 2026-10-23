using System.Text.Json;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Autostart;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Theme;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.Settings;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Settings;

public sealed class GivenASettingsViewModelWithLaunchAtStartup
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly ISettingsService settingsService = Substitute.For<ISettingsService>();
    private readonly IAutostartService autostartService = Substitute.For<IAutostartService>();
    private readonly ILocalizationService loc = Substitute.For<ILocalizationService>();
    private readonly AppSettings appSettings = new();

    public GivenASettingsViewModelWithLaunchAtStartup()
    {
        _ = settingsService.Current.Returns(appSettings);
        _ = settingsService.SaveAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _ = loc.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        _ = loc.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());
    }

    private SettingsViewModel CreateSut() => new(settingsService, Substitute.For<IThemeService>(), Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), loc, Substitute.For<IFolderPickerService>(), autostartService);

    [Fact]
    public void when_settings_are_new_then_launch_at_startup_defaults_to_on()
    {
        new AppSettings().LaunchAtStartup.ShouldBeTrue();
    }

    [Fact]
    public void when_settings_json_has_no_launch_at_startup_value_then_it_defaults_to_on()
    {
        var settings = JsonSerializer.Deserialize<AppSettings>("{\"locale\":\"en-GB\"}", JsonOpts);

        settings!.LaunchAtStartup.ShouldBeTrue();
    }

    [Fact]
    public void when_the_view_model_is_created_then_launch_at_startup_reflects_the_saved_setting()
    {
        appSettings.LaunchAtStartup = false;

        var sut = CreateSut();

        sut.LaunchAtStartup.ShouldBeFalse();
    }

    [Fact]
    public void when_launch_at_startup_is_turned_off_then_the_setting_is_saved_and_the_registration_removed()
    {
        var sut = CreateSut();

        sut.LaunchAtStartup = false;

        appSettings.LaunchAtStartup.ShouldBeFalse();
        _ = settingsService.Received(1).SaveAsync(Arg.Any<CancellationToken>());
        autostartService.Received(1).Apply(false);
    }

    [Fact]
    public void when_launch_at_startup_is_turned_back_on_then_the_registration_is_applied()
    {
        appSettings.LaunchAtStartup = false;
        var sut = CreateSut();

        sut.LaunchAtStartup = true;

        appSettings.LaunchAtStartup.ShouldBeTrue();
        autostartService.Received(1).Apply(true);
    }

    [Fact]
    public void when_the_view_model_is_created_then_the_registration_is_not_applied()
    {
        _ = CreateSut();

        autostartService.DidNotReceive().Apply(Arg.Any<bool>());
    }

    [Fact]
    public void when_settings_are_reloaded_then_launch_at_startup_updates_without_applying_the_registration()
    {
        var sut = CreateSut();
        var reloaded = new AppSettings { LaunchAtStartup = false };

        settingsService.SettingsChanged += Raise.Event<EventHandler<AppSettings>>(settingsService, reloaded);

        sut.LaunchAtStartup.ShouldBeFalse();
        autostartService.DidNotReceive().Apply(Arg.Any<bool>());
        _ = settingsService.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_the_labels_are_requested_then_they_come_from_localisation()
    {
        var sut = CreateSut();

        sut.LaunchAtStartupLabelText.ShouldBe("Settings.LaunchAtStartup.Label");
        sut.LaunchAtStartupDescriptionText.ShouldBe("Settings.LaunchAtStartup.Description");
    }
}
