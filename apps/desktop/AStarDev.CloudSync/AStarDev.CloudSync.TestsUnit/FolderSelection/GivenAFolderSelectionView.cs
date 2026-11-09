using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.FolderSelection;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels;
using AStarDev.CloudSync.ViewModels.FolderSelection;
using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.TestsUnit.FolderSelection;

public class GivenAFolderSelectionView
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly FakeFolderTreeSource tree = new();
    private readonly FakeSelectionRuleWriter writer = new();

    public GivenAFolderSelectionView() => tree.Levels = _ => Result.Success<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>([Node("Docs", FolderOriginFactory.CreateBoth(), true), Node("Music", FolderOriginFactory.CreateRemoteOnly(), false), Node("Photos", FolderOriginFactory.CreateLocalOnly(), false)]);

    [AvaloniaFact]
    public async Task when_the_dialog_is_closed_then_the_window_does_not_show_it()
    {
        var (window, _) = await ShownWindowAsync();

        window.GetVisualDescendants().OfType<FolderSelectionView>().ShouldAllBe(view => !view.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task when_the_dialog_is_open_then_the_window_shows_it_over_the_layout_and_disables_the_layout()
    {
        var (window, dialog) = await ShownWindowAsync();

        _ = dialog.ShowAsync(Option.Some(Account), TestContext.Current.CancellationToken);
        window.UpdateLayout();

        window.GetVisualDescendants().OfType<FolderSelectionView>().ShouldHaveSingleItem().IsEffectivelyVisible.ShouldBeTrue();
        window.FindControl<ContentControl>("LayoutHost").ShouldNotBeNull().IsEffectivelyEnabled.ShouldBeFalse();
    }

    [AvaloniaFact]
    public async Task when_the_tree_is_shown_then_each_folder_has_its_name_origin_and_a_check_button_with_an_accessible_name()
    {
        var (window, dialog) = await ShownWindowAsync();

        _ = dialog.ShowAsync(Option.Some(Account), TestContext.Current.CancellationToken);
        window.UpdateLayout();

        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ToList();
        var checkButtons = window.GetVisualDescendants().OfType<Button>().Where(button => Equals(button.Content, Text("FolderSelection.Glyph.Unticked"))).ToList();
        texts.ShouldContain("Docs");
        texts.ShouldContain(Text("FolderSelection.Origin.RemoteOnly"));
        texts.ShouldContain(Text("FolderSelection.Origin.LocalOnly"));
        checkButtons.Count.ShouldBe(3);
        checkButtons.Select(button => button.Command).ShouldBe(dialog.Roots.Select(node => (System.Windows.Input.ICommand)node.ToggleCommand));
        checkButtons.Select(button => Avalonia.Automation.AutomationProperties.GetName(button)).ShouldBe(dialog.Roots.Select(node => node.AccessibleName));
    }

    [AvaloniaFact]
    public async Task when_space_is_pressed_on_a_folders_check_button_then_the_folder_is_ticked_and_apply_is_enabled()
    {
        var (window, dialog) = await ShownWindowAsync();
        _ = dialog.ShowAsync(Option.Some(Account), TestContext.Current.CancellationToken);
        window.UpdateLayout();
        var button = window.GetVisualDescendants().OfType<Button>().First(candidate => Equals(candidate.Content, Text("FolderSelection.Glyph.Unticked")));

        _ = button.Focus();
        window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        window.UpdateLayout();

        (dialog.Roots[0].CheckState, dialog.ApplyCommand.CanExecute(null)).ShouldBe((FolderCheckStateFactory.CreateTicked(), true));
    }

    [AvaloniaFact]
    public async Task when_the_dialog_is_open_then_apply_and_cancel_are_bound_to_their_commands()
    {
        var (window, dialog) = await ShownWindowAsync();

        _ = dialog.ShowAsync(Option.Some(Account), TestContext.Current.CancellationToken);
        window.UpdateLayout();

        var buttons = window.GetVisualDescendants().OfType<FolderSelectionView>().Single().GetVisualDescendants().OfType<Button>().ToList();
        buttons.Single(button => Equals(button.Content, dialog.ApplyText)).Command.ShouldBeSameAs(dialog.ApplyCommand);
        buttons.Single(button => Equals(button.Content, dialog.CancelText) && button.IsEffectivelyVisible).Command.ShouldBeSameAs(dialog.CancelCommand);
    }

    [AvaloniaFact]
    public async Task when_a_level_fails_then_the_error_and_a_retry_button_show()
    {
        tree.Levels = _ => Result.Failure<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>(FolderTreeFailureFactory.CreateRemoteFailed(ProviderErrorFactory.CreateTransient()));
        var (window, dialog) = await ShownWindowAsync();

        _ = dialog.ShowAsync(Option.Some(Account), TestContext.Current.CancellationToken);
        window.UpdateLayout();

        window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ShouldContain(Text("FolderSelection.Level.Failed.Remote"));
        window.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, dialog.RetryText) && button.IsEffectivelyVisible).Command.ShouldBeSameAs(dialog.RetryCommand);
    }

    private static string Text(string key) => TestData.CreateLocalization().GetLocal(key);

    private static FolderTreeNode Node(string name, FolderOrigin origin, bool children) =>
        FolderTreeNodeFactory.Create(name, RelativePaths.Of(name), Option.None<RemoteItemId>(), origin, children ? FolderChildrenFactory.CreateMayHaveChildFolders() : FolderChildrenFactory.CreateNoChildFolders(), SelectionStateFactory.CreateUncovered(), FolderNameClashFactory.CreateUnique());

    private async Task<(MainWindow Window, FolderSelectionViewModel Dialog)> ShownWindowAsync()
    {
        var dialog = TestData.CreateFolderSelection(tree, new FakeSelectionRuleReader(), writer, new FakeSyncPairReader(), new FakeAccountStore());
        var window = new MainWindow { DataContext = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings(), dialog) };
        window.Show();

        return (window, dialog);
    }
}
