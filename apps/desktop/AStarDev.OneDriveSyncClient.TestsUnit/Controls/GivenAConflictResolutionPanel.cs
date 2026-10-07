using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Conflicts;
using AStarDev.OneDriveSyncClient.Controls;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Localization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Controls;

[Collection("AvaloniaHeadless")]
public sealed class GivenAConflictResolutionPanel
{
    private const string SelectedClassName = "selected";

    private static ConflictItemViewModel BuildViewModel()
    {
        var conflict = new SyncConflict
        {
            Remote = RemoteItemRefFactory.Create(new AccountId("account-123"), new OneDriveFolderId("folder-456"), new OneDriveItemId("item-789")),
            Target = SyncFileTargetFactory.Create("/home/user/docs/report.pdf", "docs/report.pdf"),
            Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow.AddHours(-1), 1024L, DateTimeOffset.UtcNow, 2048L),
        };
        var loc = Substitute.For<ILocalizationService>();
        loc.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));

        return new ConflictItemViewModel(conflict, Substitute.For<ISyncService>(), loc);
    }

    private static void Show(ConflictResolutionPanel sut) => new Window { Content = sut }.Show();

    private static List<Button> PolicyButtons(ConflictResolutionPanel sut) => sut.GetVisualDescendants().OfType<Button>().Where(button => button.Tag is ConflictPolicy).ToList();

    [AvaloniaFact]
    public void when_remote_wins_is_clicked_then_only_remote_wins_button_has_selected_class()
    {
        var viewModel = BuildViewModel();
        var sut = new ConflictResolutionPanel { DataContext = viewModel };
        Show(sut);

        PolicyButtons(sut).Single(button => (ConflictPolicy)button.Tag! == ConflictPolicy.RemoteWins).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        var selected = PolicyButtons(sut).Where(button => button.Classes.Contains(SelectedClassName)).Select(button => (ConflictPolicy)button.Tag!).ToList();
        selected.ShouldBe([ConflictPolicy.RemoteWins]);
    }

    [AvaloniaFact]
    public void when_panel_is_first_shown_then_default_policy_button_has_selected_class()
    {
        var sut = new ConflictResolutionPanel { DataContext = BuildViewModel() };
        Show(sut);

        var selected = PolicyButtons(sut).Where(button => button.Classes.Contains(SelectedClassName)).Select(button => (ConflictPolicy)button.Tag!).ToList();

        selected.ShouldBe([ConflictPolicy.Ignore]);
    }
}
