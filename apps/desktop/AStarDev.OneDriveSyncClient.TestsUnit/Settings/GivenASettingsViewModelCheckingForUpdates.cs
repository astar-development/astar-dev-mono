using System.Globalization;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Autostart;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Theme;
using AStarDev.Localization;
using AStarDev.OneDriveSyncClient.Settings;
using Velopack;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Settings;

public sealed class GivenASettingsViewModelCheckingForUpdates
{
    private readonly ILocalizationService loc = Substitute.For<ILocalizationService>();
    private readonly IUpdateNotificationService updateNotificationService = Substitute.For<IUpdateNotificationService>();
    private readonly SettingsViewModel sut;

    public GivenASettingsViewModelCheckingForUpdates()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.Current.Returns(new AppSettings());
        loc.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        loc.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => x.ArgAt<string>(0));
        loc.AvailableCultures.Returns([CultureInfo.GetCultureInfo("en-GB")]);
        sut = new SettingsViewModel(settingsService, Substitute.For<IThemeService>(), Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), loc, Substitute.For<IFolderPickerService>(), Substitute.For<IAutostartService>(), updateNotificationService);
    }

    private static UpdateInfo CreateUpdateInfo()
    {
        var asset = new VelopackAsset { Version = SemanticVersion.Parse("1.2.3") };

        return new UpdateInfo(asset, false, null, []);
    }

    [Fact]
    public void when_created_then_update_status_is_empty()
    {
        sut.UpdateStatusText.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_check_for_updates_runs_then_the_update_service_is_checked_once()
    {
        updateNotificationService.CheckNowAsync(Arg.Any<CancellationToken>()).Returns(UpdateCheckResultFactory.CreateUpToDate());

        await sut.CheckForUpdatesCommand.ExecuteAsync(null);

        await updateNotificationService.Received(1).CheckNowAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_up_to_date_then_status_says_up_to_date()
    {
        updateNotificationService.CheckNowAsync(Arg.Any<CancellationToken>()).Returns(UpdateCheckResultFactory.CreateUpToDate());

        await sut.CheckForUpdatesCommand.ExecuteAsync(null);

        sut.UpdateStatusText.ShouldBe("Settings.Updates.UpToDate");
    }

    [Fact]
    public async Task when_not_installed_then_status_says_not_installed()
    {
        updateNotificationService.CheckNowAsync(Arg.Any<CancellationToken>()).Returns(UpdateCheckResultFactory.CreateNotInstalled());

        await sut.CheckForUpdatesCommand.ExecuteAsync(null);

        sut.UpdateStatusText.ShouldBe("Settings.Updates.NotInstalled");
    }

    [Fact]
    public async Task when_check_fails_then_status_says_failed()
    {
        updateNotificationService.CheckNowAsync(Arg.Any<CancellationToken>()).Returns(UpdateCheckResultFactory.CreateFailed("boom"));

        await sut.CheckForUpdatesCommand.ExecuteAsync(null);

        sut.UpdateStatusText.ShouldBe("Settings.Updates.Failed");
    }

    [Fact]
    public async Task when_update_is_found_then_status_says_found()
    {
        updateNotificationService.CheckNowAsync(Arg.Any<CancellationToken>()).Returns(UpdateCheckResultFactory.CreateFound(CreateUpdateInfo()));

        await sut.CheckForUpdatesCommand.ExecuteAsync(null);

        sut.UpdateStatusText.ShouldBe("Settings.Updates.Found");
    }

    [Fact]
    public async Task when_the_check_is_running_then_status_says_checking_and_the_command_cannot_run_again()
    {
        var gate = new TaskCompletionSource<UpdateCheckResult>();
        updateNotificationService.CheckNowAsync(Arg.Any<CancellationToken>()).Returns(gate.Task);

        var running = sut.CheckForUpdatesCommand.ExecuteAsync(null);

        sut.UpdateStatusText.ShouldBe("Settings.Updates.Checking");
        sut.CheckForUpdatesCommand.CanExecute(null).ShouldBeFalse();
        gate.SetResult(UpdateCheckResultFactory.CreateUpToDate());
        await running;
    }

    [Theory]
    [InlineData("CheckForUpdatesLabelText", "Settings.Updates.Label")]
    [InlineData("CheckForUpdatesDescriptionText", "Settings.Updates.Description")]
    [InlineData("CheckForUpdatesButtonText", "Settings.Updates.Button")]
    public void when_text_property_is_read_then_it_comes_from_the_localisation_service(string propertyName, string expectedKey)
    {
        var value = (string?)typeof(SettingsViewModel).GetProperty(propertyName)?.GetValue(sut);

        value.ShouldBe(expectedKey);
    }
}
