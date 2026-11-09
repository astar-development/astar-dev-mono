using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.FolderSelection;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheFolderSelectionSnapshots
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    [AvaloniaTheory]
    [InlineData("tree", "dark")]
    [InlineData("tree", "light")]
    [InlineData("failed", "dark")]
    [InlineData("failed", "light")]
    [InlineData("confirm", "dark")]
    [InlineData("confirm", "light")]
    public async Task when_rendered_then_the_folder_selection_dialog_matches_the_baseline(string step, string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var tree = new FakeFolderTreeSource { Levels = position => step == "failed" ? Failure() : Levels(position) };
        var rules = new FakeSelectionRuleReader { Rules = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([Rule("Docs/Work", SyncDirectionFactory.CreateTwoWay()), Rule("Music", SyncDirectionFactory.CreateUploadOnly())]) };
        var pairs = new FakeSyncPairReader { Pairs = _ => Result.Success<IReadOnlyList<SyncPair>, ErrorResponse>(step == "confirm" ? [SyncedPair("Music")] : []) };
        var dialog = TestData.CreateFolderSelection(tree, rules, new FakeSelectionRuleWriter(), pairs, new FakeAccountStore());
        _ = dialog.ShowAsync(Option.Some(Account), TestContext.Current.CancellationToken);
        if (step == "tree")
            await dialog.Roots.First(node => node.Name == "Docs").ExpandCommand.ExecuteAsync(null);

        if (step == "confirm")
        {
            dialog.Roots.First(node => node.Name == "Music").ToggleCommand.Execute(null);
            await dialog.ApplyCommand.ExecuteAsync(null);
        }

        var window = new Window
        {
            Content = new Border
            {
                Padding = new Avalonia.Thickness(16),
                [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CloudSyncBackgroundBrush"),
                Child = new FolderSelectionView { DataContext = dialog }
            }
        };

        Snapshot.ShouldMatchBaseline(window, theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light, $"control-folder-selection-{step}-{theme}", 640, 640);
    }

    private static Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure> Failure() => Result.Failure<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>(FolderTreeFailureFactory.CreateBothFailed(ProviderErrorFactory.CreateTransient(), LocalFolderErrorFactory.CreateRootUnavailable()));

    private static Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure> Levels(FolderTreePosition position) => Result.Success<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>(position is FolderTreePosition.Root
        ? [Node("Docs", FolderOriginFactory.CreateBoth(), true, null), Node("Music", FolderOriginFactory.CreateRemoteOnly(), false, null), Node("Photos", FolderOriginFactory.CreateLocalOnly(), false, null), Node("Projects", FolderOriginFactory.CreateBoth(), false, null, clash: true)]
        : [Node("Work", FolderOriginFactory.CreateBoth(), false, "Docs"), Node("Notes", FolderOriginFactory.CreateLocalOnly(), false, "Docs")]);

    private static FolderTreeNode Node(string name, FolderOrigin origin, bool children, string? parent, bool clash = false) =>
        FolderTreeNodeFactory.Create(name, RelativePaths.Of(parent is null ? name : $"{parent}/{name}"), Option.None<RemoteItemId>(), origin, children ? FolderChildrenFactory.CreateMayHaveChildFolders() : FolderChildrenFactory.CreateNoChildFolders(), SelectionStateFactory.CreateUncovered(), clash ? FolderNameClashFactory.CreateCaseClash() : FolderNameClashFactory.CreateUnique());

    private static SelectionRule Rule(string path, SyncDirection direction) => SelectionRuleFactory.Create(Account, RelativePaths.Of(path), Option.None<RemoteItemId>(), SelectionRuleKindFactory.CreateInclude(direction));

    private static SyncPair SyncedPair(string path) => SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), Account, path, RelativePaths.Of(path), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateUploadOnly(), SyncStatusFactory.CreateUpToDate(), 0, Option.Some(TestData.Now));
}
