using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;
using Velopack;

namespace AStar.Dev.Velopack.Publishing.Avalonia.TestsUnit.Updates;

public sealed class GivenAnUpdateAvailableViewModel
{
    private static UpdateInfo CreateUpdateInfo(string version = "1.2.3", string releaseNotes = "## What's new", long sizeInBytes = 0)
    {
        var asset = new VelopackAsset { Version = SemanticVersion.Parse(version), NotesMarkdown = releaseNotes, Size = sizeInBytes };

        return new UpdateInfo(asset, false, null, []);
    }

    private static (UpdateAvailableViewModel ViewModel, IVelopackUpdateService UpdateCheckService) CreateSut(UpdateInfo? updateInfo = null)
    {
        var updateCheckService = Substitute.For<IVelopackUpdateService>();
        var textProvider = new FakeUpdateDialogTextProvider();
        var logger = Substitute.For<ILogger<UpdateAvailableViewModel>>();
        var viewModel = new UpdateAvailableViewModel(updateInfo ?? CreateUpdateInfo(), updateCheckService, textProvider, logger);

        return (viewModel, updateCheckService);
    }

    [Fact]
    public void when_constructed_then_title_is_the_echoed_text_provider_value()
    {
        var (viewModel, _) = CreateSut();

        viewModel.Title.ShouldBe("Update.Title");
    }

    [Fact]
    public void when_constructed_then_restart_now_label_is_the_echoed_text_provider_value()
    {
        var (viewModel, _) = CreateSut();

        viewModel.RestartNowLabel.ShouldBe("Update.RestartNow");
    }

    [Fact]
    public void when_constructed_then_later_label_is_the_echoed_text_provider_value()
    {
        var (viewModel, _) = CreateSut();

        viewModel.LaterLabel.ShouldBe("Update.Later");
    }

    [Fact]
    public void when_constructed_then_target_version_is_populated_from_update_info()
    {
        var (viewModel, _) = CreateSut(CreateUpdateInfo(version: "2.0.0"));

        viewModel.TargetVersion.ShouldBe("2.0.0");
    }

    [Fact]
    public void when_constructed_then_release_notes_are_populated_from_update_info()
    {
        var (viewModel, _) = CreateSut(CreateUpdateInfo(releaseNotes: "## Fixed a bug"));

        viewModel.ReleaseNotes.ShouldBe("## Fixed a bug");
    }

    [Fact]
    public void when_constructed_then_message_includes_the_target_version()
    {
        var (viewModel, _) = CreateSut(CreateUpdateInfo(version: "2.0.0"));

        viewModel.Message.ShouldBe("Update.Message:2.0.0");
    }

    [Fact]
    public void when_later_command_executed_then_close_requested_is_raised()
    {
        var (viewModel, _) = CreateSut();
        bool closeRequested = false;
        viewModel.CloseRequested += (_, _) => closeRequested = true;

        viewModel.LaterCommand.Execute(null);

        closeRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task when_restart_now_command_executed_then_updates_are_downloaded_then_applied()
    {
        var updateInfo = CreateUpdateInfo();
        var (viewModel, updateCheckService) = CreateSut(updateInfo);

        await viewModel.RestartNowCommand.ExecuteAsync(null);

        Received.InOrder(() =>
        {
            updateCheckService.DownloadUpdatesAsync(updateInfo, cancellationToken: Arg.Any<CancellationToken>());
            updateCheckService.ApplyUpdatesAndRestart(updateInfo);
        });
    }

    [Fact]
    public async Task when_restart_now_command_executed_then_is_busy_ends_false()
    {
        var (viewModel, _) = CreateSut();

        await viewModel.RestartNowCommand.ExecuteAsync(null);

        viewModel.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public async Task when_download_fails_then_error_message_is_populated_and_exception_does_not_propagate()
    {
        var (viewModel, updateCheckService) = CreateSut();
        updateCheckService.DownloadUpdatesAsync(Arg.Any<UpdateInfo>(), cancellationToken: Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("offline"));

        await viewModel.RestartNowCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.ShouldBe("offline");
    }

    [Fact]
    public async Task when_download_fails_then_apply_updates_and_restart_is_never_called()
    {
        var (viewModel, updateCheckService) = CreateSut();
        updateCheckService.DownloadUpdatesAsync(Arg.Any<UpdateInfo>(), cancellationToken: Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("offline"));

        await viewModel.RestartNowCommand.ExecuteAsync(null);

        updateCheckService.DidNotReceive().ApplyUpdatesAndRestart(Arg.Any<UpdateInfo>());
    }

    [Fact]
    public async Task when_the_download_reports_progress_then_the_percentage_is_exposed()
    {
        var (viewModel, updateCheckService) = CreateSut(CreateUpdateInfo(sizeInBytes: 48 * 1024 * 1024));
        ReportProgressWhenDownloading(updateCheckService, 50);

        await viewModel.RestartNowCommand.ExecuteAsync(null);

        viewModel.DownloadProgress.ShouldBe(50);
    }

    [Fact]
    public async Task when_the_download_reports_progress_then_the_text_shows_the_megabytes_downloaded_and_total()
    {
        var (viewModel, updateCheckService) = CreateSut(CreateUpdateInfo(sizeInBytes: 48 * 1024 * 1024));
        ReportProgressWhenDownloading(updateCheckService, 50);

        await viewModel.RestartNowCommand.ExecuteAsync(null);

        viewModel.DownloadProgressText.ShouldBe("50% · 24.0 / 48.0 MB");
    }

    [Fact]
    public async Task when_the_update_is_a_delta_then_the_total_is_the_size_of_the_deltas()
    {
        var delta = new VelopackAsset { Version = SemanticVersion.Parse("1.2.3"), Size = 10 * 1024 * 1024 };
        var updateInfo = new UpdateInfo(new VelopackAsset { Version = SemanticVersion.Parse("1.2.3"), Size = 48 * 1024 * 1024 }, false, null, [delta]);
        var (viewModel, updateCheckService) = CreateSut(updateInfo);
        ReportProgressWhenDownloading(updateCheckService, 100);

        await viewModel.RestartNowCommand.ExecuteAsync(null);

        viewModel.DownloadProgressText.ShouldBe("100% · 10.0 / 10.0 MB");
    }

    private static void ReportProgressWhenDownloading(IVelopackUpdateService updateCheckService, int percent)
        => updateCheckService.DownloadUpdatesAsync(Arg.Any<UpdateInfo>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                call.ArgAt<Action<int>?>(1)?.Invoke(percent);

                return Task.CompletedTask;
            });
}
