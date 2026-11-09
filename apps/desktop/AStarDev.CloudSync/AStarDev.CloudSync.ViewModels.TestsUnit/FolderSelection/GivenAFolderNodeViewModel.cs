using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.FolderSelection;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.FolderSelection;

public class GivenAFolderNodeViewModel
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly FakeFolderTreeSource tree = new();
    private readonly FakeSelectionRuleReader rules = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_a_folder_is_closed_then_the_expander_points_right_and_when_open_it_points_down()
    {
        var folder = await FirstRootAsync();
        var closed = folder.ExpanderGlyph;

        await folder.ToggleExpandedCommand.ExecuteAsync(null);

        (closed, folder.ExpanderGlyph).ShouldBe((Text("FolderSelection.Glyph.Collapsed"), Text("FolderSelection.Glyph.Expanded")));
    }

    [Fact]
    public async Task when_an_open_folder_is_toggled_then_it_closes_without_loading_again()
    {
        var folder = await FirstRootAsync();
        await folder.ToggleExpandedCommand.ExecuteAsync(null);
        var requestedAfterOpening = tree.Requested.Count;

        await folder.ToggleExpandedCommand.ExecuteAsync(null);

        (folder.IsExpanded, tree.Requested.Count).ShouldBe((false, requestedAfterOpening));
    }

    [Fact]
    public async Task when_a_closed_loaded_folder_is_toggled_then_it_opens_without_loading_again()
    {
        var folder = await FirstRootAsync();
        await folder.ToggleExpandedCommand.ExecuteAsync(null);
        await folder.ToggleExpandedCommand.ExecuteAsync(null);
        var requested = tree.Requested.Count;

        await folder.ToggleExpandedCommand.ExecuteAsync(null);

        (folder.IsExpanded, tree.Requested.Count).ShouldBe((true, requested));
    }

    [Fact]
    public async Task when_the_check_state_changes_then_the_checkbox_symbol_follows()
    {
        var folder = await FirstRootAsync();
        var unticked = folder.CheckGlyph;

        folder.ToggleCommand.Execute(null);

        (unticked, folder.CheckGlyph).ShouldBe((Text("FolderSelection.Glyph.Unticked"), Text("FolderSelection.Glyph.Ticked")));
    }

    [Fact]
    public async Task when_a_rule_is_below_the_folder_then_the_symbol_is_the_partial_one()
    {
        rules.Rules = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([SelectionRuleFactory.Create(Account, RelativePaths.Of("Docs/Sub"), Option.None<RemoteItemId>(), SelectionRuleKindFactory.CreateInclude(SyncDirectionFactory.CreateTwoWay()))]);

        var folder = await FirstRootAsync();

        folder.CheckGlyph.ShouldBe(Text("FolderSelection.Glyph.Partial"));
    }

    private static string Text(string key) => TestData.CreateLocalization().GetLocal(key);

    private async Task<FolderNodeViewModel> FirstRootAsync()
    {
        tree.Levels = position => position is FolderTreePosition.Root
            ? Result.Success<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>([FolderTreeNodeFactory.Create("Docs", RelativePaths.Of("Docs"), Option.None<RemoteItemId>(), FolderOriginFactory.CreateBoth(), FolderChildrenFactory.CreateMayHaveChildFolders(), SelectionStateFactory.CreateUncovered(), FolderNameClashFactory.CreateUnique())])
            : Result.Success<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>([]);
        var dialog = TestData.CreateFolderSelection(tree, rules, new FakeSelectionRuleWriter(), new FakeSyncPairReader(), new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([]) });
        _ = dialog.ShowAsync(Option.Some(Account), CancellationToken);

        return await Task.FromResult(dialog.Roots.Single());
    }
}
