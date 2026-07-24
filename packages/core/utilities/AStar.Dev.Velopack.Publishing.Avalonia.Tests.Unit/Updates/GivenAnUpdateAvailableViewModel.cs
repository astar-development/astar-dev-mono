using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;
using Velopack;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Tests.Unit.Updates;

internal sealed class GivenAnUpdateAvailableViewModel
{
    private static UpdateInfo CreateUpdateInfo(string version = "1.2.3", string releaseNotes = "## What's new")
    {
        var asset = new VelopackAsset { Version = SemanticVersion.Parse(version), NotesMarkdown = releaseNotes };

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
}
