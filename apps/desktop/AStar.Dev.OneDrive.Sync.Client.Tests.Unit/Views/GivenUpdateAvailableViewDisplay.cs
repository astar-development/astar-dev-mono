using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Updates;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Updates;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Microsoft.Extensions.Logging;
using Velopack;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

public sealed class GivenUpdateAvailableViewDisplay
{
    private static UpdateInfo CreateUpdateInfo(string version = "1.2.3")
    {
        var asset = new VelopackAsset { Version = SemanticVersion.Parse(version), NotesMarkdown = "## Fixed a bug" };

        return new UpdateInfo(asset, false, null, []);
    }

    private static UpdateAvailableViewModel CreateViewModel(UpdateInfo? updateInfo = null)
    {
        var updateCheckService = Substitute.For<IUpdateCheckService>();
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localization.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());
        var logger = Substitute.For<ILogger<UpdateAvailableViewModel>>();

        return new UpdateAvailableViewModel(updateInfo ?? CreateUpdateInfo(), updateCheckService, localization, logger);
    }

    private static UpdateAvailableView CreateViewWithViewModel(UpdateAvailableViewModel viewModel)
    {
        var view = new UpdateAvailableView { DataContext = viewModel };
        view.Measure(new(480, 420));
        view.Arrange(new(0, 0, 480, 420));

        return view;
    }

    [AvaloniaFact]
    public void when_rendered_then_title_text_block_shows_localized_title()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var titleBlock = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.Text == "Update.Title");
        titleBlock.ShouldNotBeNull("title TextBlock must display the echoed localization key for Update.Title");
    }

    [AvaloniaFact]
    public void when_rendered_then_release_notes_text_block_shows_update_info_notes()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var notesBlock = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.Text == "## Fixed a bug");
        notesBlock.ShouldNotBeNull("release notes TextBlock must display the UpdateInfo's NotesMarkdown");
    }

    [AvaloniaFact]
    public void when_rendered_then_restart_now_button_is_bound_to_restart_now_command()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var restartButton = sut.GetLogicalDescendants().OfType<Button>().FirstOrDefault(b => b.Command == viewModel.RestartNowCommand);
        restartButton.ShouldNotBeNull();
    }

    [AvaloniaFact]
    public void when_rendered_then_later_button_is_bound_to_later_command()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var laterButton = sut.GetLogicalDescendants().OfType<Button>().FirstOrDefault(b => b.Command == viewModel.LaterCommand);
        laterButton.ShouldNotBeNull();
    }

    [AvaloniaFact]
    public void when_no_error_then_error_text_block_is_hidden()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var errorBlock = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.Text is null);
        errorBlock.ShouldNotBeNull("error TextBlock bound to ErrorMessage should be present, with a null Text while ErrorMessage is null");
        errorBlock.IsVisible.ShouldBeFalse("error TextBlock should be hidden when ErrorMessage is null");
    }

    [AvaloniaFact]
    public void when_error_message_is_set_then_error_text_block_becomes_visible()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.ErrorMessage = "offline";

        var errorBlock = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.Text == "offline");
        errorBlock.ShouldNotBeNull();
        errorBlock.IsVisible.ShouldBeTrue("error TextBlock must become visible once ErrorMessage is populated");
    }

    [AvaloniaFact]
    public void when_is_busy_then_restart_now_button_is_disabled()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.IsBusy = true;

        var restartButton = sut.GetLogicalDescendants().OfType<Button>().First(b => b.Command == viewModel.RestartNowCommand);
        restartButton.IsEnabled.ShouldBeFalse("Restart Now must be disabled while a download is in progress");
    }
}
