using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.FolderSelection;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.FolderSelection;

public class GivenAFolderSelectionViewModel
{
    private readonly FakeFolderTreeSource tree = new();
    private readonly FakeSelectionRuleReader rules = new();
    private readonly FakeSelectionRuleWriter writer = new();
    private FakeSyncPairReader pairReader = new();
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly FakeAccountStore accounts = new() { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredPersonal()]) };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_shown_for_an_account_then_the_dialog_opens_with_its_name_and_first_level()
    {
        tree.Levels = _ => Level(Node("Docs"), Node("Photos"));

        var dialog = await OpenAsync();

        (dialog.IsOpen, dialog.IsShowingTree, dialog.AccountNameText).ShouldBe((true, true, "Personal"));
        dialog.Roots.Select(node => node.Name).ToList().ShouldBe(["Docs", "Photos"]);
    }

    [Fact]
    public async Task when_shown_without_an_account_then_the_accounts_are_listed_and_choosing_one_opens_its_tree()
    {
        tree.Levels = _ => Level(Node("Docs"));
        var dialog = Create();

        _ = dialog.ShowAsync(Option.None<AccountId>(), CancellationToken);
        var listed = (dialog.IsChoosingAccount, dialog.Accounts.Select(choice => choice.Name).ToList());
        await dialog.Accounts.Single().ChooseCommand.ExecuteAsync(null);

        listed.IsChoosingAccount.ShouldBeTrue();
        listed.Item2.ShouldBe(["Personal"]);
        (dialog.IsShowingTree, dialog.AccountNameText, dialog.Roots.Count).ShouldBe((true, "Personal", 1));
    }

    [Fact]
    public void when_the_accounts_cannot_be_read_then_a_message_shows_and_no_account_is_listed()
    {
        var dialog = new FolderSelectionViewModel(tree, rules, writer, new SyncSelectionEvaluator(), pairReader, new FakeAccountStore { All = () => Result.Failure<IReadOnlyList<StoredAccount>, ErrorResponse>(new ErrorResponse("failed")) }, TestData.CreateLocalization());

        _ = dialog.ShowAsync(Option.None<AccountId>(), CancellationToken);

        (dialog.HasMessage, dialog.MessageText, dialog.Accounts.Count).ShouldBe((true, Text("FolderSelection.Failure.Accounts"), 0));
    }

    [Fact]
    public async Task when_the_first_level_fails_then_the_error_names_the_failed_side_and_retry_loads_it()
    {
        tree.Levels = _ => Result.Failure<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>(FolderTreeFailureFactory.CreateRemoteFailed(ProviderErrorFactory.CreateTransient()));
        var dialog = await OpenAsync();
        var failed = (dialog.HasRootError, dialog.RootErrorText, dialog.Roots.Count);
        tree.Levels = _ => Level(Node("Docs"));

        await dialog.RetryCommand.ExecuteAsync(null);

        failed.ShouldBe((true, Text("FolderSelection.Level.Failed.Remote"), 0));
        (dialog.HasRootError, dialog.Roots.Count).ShouldBe((false, 1));
    }

    [Fact]
    public async Task when_the_saved_choices_cannot_be_read_then_the_error_says_so_and_no_folders_show()
    {
        rules.Rules = Result.Failure<IReadOnlyList<SelectionRule>, ErrorResponse>(new ErrorResponse("rules"));
        tree.Levels = _ => Level(Node("Docs"));

        var dialog = await OpenAsync();

        (dialog.RootErrorText, dialog.Roots.Count).ShouldBe((Text("FolderSelection.Level.Failed.Rules"), 0));
    }

    [Fact]
    public async Task when_rules_exist_then_each_folder_shows_ticked_unticked_or_partially_ticked()
    {
        tree.Levels = _ => Level(Node("Docs"), Node("Misc"), Node("Photos"));
        Stored(SelectionRuleFor("Docs", included: true), SelectionRuleFor("Photos", included: true), SelectionRuleFor("Photos/Private", included: false));

        var dialog = await OpenAsync();

        dialog.Roots.Select(node => node.CheckState).ToList().ShouldBe([FolderCheckStateFactory.CreateTicked(), FolderCheckStateFactory.CreateUnticked(), FolderCheckStateFactory.CreatePartiallyTicked()]);
        dialog.Roots[0].AccessibleName.ShouldBe(TestData.CreateLocalization().GetLocal("FolderSelection.Node.AccessibleName", "Docs", Text("FolderSelection.Check.Ticked")));
    }

    [Fact]
    public async Task when_a_folder_is_opened_then_only_its_children_load_once_and_they_show_their_states()
    {
        tree.Levels = position => position is FolderTreePosition.Root ? Level(Node("Docs", children: true)) : Level(Node("Sub", "Docs"));
        Stored(SelectionRuleFor("Docs", included: true));
        var dialog = await OpenAsync();
        var docs = dialog.Roots.Single();

        await docs.ExpandCommand.ExecuteAsync(null);
        await docs.ExpandCommand.ExecuteAsync(null);

        (tree.Requested.Count, docs.IsExpanded, docs.Children.Select(child => (child.Path.Value, child.CheckState)).ToList()).ShouldBe((2, true, [("Docs/Sub", FolderCheckStateFactory.CreateTicked())]));
    }

    [Fact]
    public async Task when_a_folders_children_fail_to_load_then_the_error_names_the_side_and_opening_again_retries()
    {
        tree.Levels = position => position is FolderTreePosition.Root ? Level(Node("Docs", children: true)) : Result.Failure<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>(FolderTreeFailureFactory.CreateLocalFailed(LocalFolderErrorFactory.CreateAccessDenied()));
        var dialog = await OpenAsync();
        var docs = dialog.Roots.Single();

        await docs.ExpandCommand.ExecuteAsync(null);
        var failed = (docs.HasLoadError, docs.LoadErrorText, docs.Children.Count);
        tree.Levels = position => position is FolderTreePosition.Root ? Level(Node("Docs", children: true)) : Level(Node("Sub", "Docs"));
        await docs.ExpandCommand.ExecuteAsync(null);

        failed.ShouldBe((true, Text("FolderSelection.Level.Failed.Local"), 0));
        (docs.HasLoadError, docs.Children.Count).ShouldBe((false, 1));
    }

    [Fact]
    public async Task when_an_unticked_folder_is_ticked_then_a_two_way_include_is_staged_and_nothing_is_saved()
    {
        tree.Levels = _ => Level(Node("Docs", remoteId: "r-docs"));
        var dialog = await OpenAsync();

        dialog.Roots.Single().ToggleCommand.Execute(null);

        dialog.Roots.Single().CheckState.ShouldBe(FolderCheckStateFactory.CreateTicked());
        dialog.HasChanges.ShouldBeTrue();
        dialog.ApplyCommand.CanExecute(null).ShouldBeTrue();
        writer.Applied.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_folder_inside_a_pair_is_ticked_then_it_takes_the_pairs_direction()
    {
        tree.Levels = position => position is FolderTreePosition.Root ? Level(Node("Docs", children: true)) : Level(Node("X", "Docs"));
        Stored(SelectionRuleFor("Docs", included: true, SyncDirectionFactory.CreateUploadOnly()), SelectionRuleFor("Docs/X", included: false));
        var dialog = await OpenAsync();
        await dialog.Roots.Single().ExpandCommand.ExecuteAsync(null);

        dialog.Roots.Single().Children.Single().ToggleCommand.Execute(null);
        dialog.ApplyCommand.Execute(null);

        var change = writer.Applied.Single().OfType<SelectionChange.IncludeFolder>().Single();
        (change.Path.Value, change.Direction).ShouldBe(("Docs/X", SyncDirectionFactory.CreateUploadOnly()));
    }

    [Fact]
    public async Task when_a_ticked_folder_is_unticked_then_an_exclude_is_staged()
    {
        tree.Levels = _ => Level(Node("Docs"));
        Stored(SelectionRuleFor("Docs", included: true));
        var dialog = await OpenAsync();

        dialog.Roots.Single().ToggleCommand.Execute(null);

        (dialog.Roots.Single().CheckState, dialog.HasChanges).ShouldBe((FolderCheckStateFactory.CreateUnticked(), true));
    }

    [Fact]
    public async Task when_a_partially_ticked_folder_is_toggled_then_it_becomes_fully_ticked()
    {
        tree.Levels = _ => Level(Node("Docs"));
        Stored(SelectionRuleFor("Docs/Sub", included: true));
        var dialog = await OpenAsync();

        dialog.Roots.Single().ToggleCommand.Execute(null);

        dialog.Roots.Single().CheckState.ShouldBe(FolderCheckStateFactory.CreateTicked());
    }

    [Fact]
    public async Task when_a_folder_is_toggled_twice_then_there_are_no_changes_to_apply()
    {
        tree.Levels = _ => Level(Node("Docs"));
        var dialog = await OpenAsync();

        dialog.Roots.Single().ToggleCommand.Execute(null);
        dialog.Roots.Single().ToggleCommand.Execute(null);

        (dialog.HasChanges, dialog.ApplyCommand.CanExecute(null)).ShouldBe((false, false));
    }

    [Fact]
    public async Task when_a_folder_has_a_case_clash_then_it_cannot_be_ticked_and_shows_a_warning()
    {
        tree.Levels = _ => Level(Node("Docs", clash: true));
        var dialog = await OpenAsync();
        var docs = dialog.Roots.Single();

        docs.ToggleCommand.Execute(null);

        (docs.CanToggle, docs.ToggleCommand.CanExecute(null), docs.CheckState, docs.CaseClashText, dialog.HasChanges).ShouldBe((false, false, FolderCheckStateFactory.CreateUnticked(), Text("FolderSelection.CaseClash"), false));
    }

    [Fact]
    public async Task when_a_ticked_folder_has_a_case_clash_then_it_can_still_be_unticked()
    {
        tree.Levels = _ => Level(Node("Docs", clash: true));
        Stored(SelectionRuleFor("Docs", included: true));
        var dialog = await OpenAsync();

        dialog.Roots.Single().ToggleCommand.Execute(null);

        dialog.Roots.Single().CheckState.ShouldBe(FolderCheckStateFactory.CreateUnticked());
    }

    [Fact]
    public async Task when_a_folder_tops_a_pair_then_its_direction_shows_and_changing_it_stages_the_next_one()
    {
        tree.Levels = _ => Level(Node("Docs"), Node("Other"));
        Stored(SelectionRuleFor("Docs", included: true));
        var dialog = await OpenAsync();
        var docs = dialog.Roots[0];
        var before = (docs.IsPairRoot, docs.DirectionText);

        docs.ChangeDirectionCommand.Execute(null);

        before.ShouldBe((true, Text("Direction.TwoWay")));
        (docs.DirectionText, dialog.HasChanges, dialog.Roots[1].IsPairRoot, dialog.Roots[1].ChangeDirectionCommand.CanExecute(null)).ShouldBe((Text("Direction.UploadOnly"), true, false, false));
    }

    [Fact]
    public async Task when_cancel_is_pressed_then_the_changes_are_thrown_away_and_the_dialog_closes()
    {
        tree.Levels = _ => Level(Node("Docs"));
        var dialog = Create();
        var shown = dialog.ShowAsync(Option.Some(Account), CancellationToken);
        dialog.Roots.Single().ToggleCommand.Execute(null);

        dialog.CancelCommand.Execute(null);
        await shown.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        (dialog.IsOpen, writer.Applied.Count).ShouldBe((false, 0));
    }

    [Fact]
    public async Task when_apply_is_pressed_then_the_staged_changes_are_saved_in_order_and_the_dialog_closes()
    {
        tree.Levels = _ => Level(Node("Docs"), Node("Photos"));
        var dialog = Create();
        var shown = dialog.ShowAsync(Option.Some(Account), CancellationToken);
        dialog.Roots[0].ToggleCommand.Execute(null);
        dialog.Roots[1].ToggleCommand.Execute(null);

        await dialog.ApplyCommand.ExecuteAsync(null);
        await shown.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        writer.Applied.Single().Select(change => ((SelectionChange.IncludeFolder)change).Path.Value).ToList().ShouldBe(["Docs", "Photos"]);
        dialog.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public async Task when_saving_fails_then_the_dialog_stays_open_with_a_message_and_the_staged_changes()
    {
        tree.Levels = _ => Level(Node("Docs"));
        writer.Outcome = Result.Failure<Unit, ErrorResponse>(new ErrorResponse("failed"));
        var dialog = await OpenAsync();
        dialog.Roots.Single().ToggleCommand.Execute(null);

        await dialog.ApplyCommand.ExecuteAsync(null);

        (dialog.IsOpen, dialog.IsShowingTree, dialog.MessageText, dialog.HasChanges).ShouldBe((true, true, Text("FolderSelection.Failure.Save"), true));
    }

    [Fact]
    public async Task when_a_folder_that_has_synced_is_unticked_then_apply_asks_first_and_saves_only_after_confirming()
    {
        tree.Levels = _ => Level(Node("Docs"));
        Stored(SelectionRuleFor("Docs", included: true));
        Synced("Docs");
        var dialog = Create();
        var shown = dialog.ShowAsync(Option.Some(Account), CancellationToken);
        dialog.Roots.Single().ToggleCommand.Execute(null);

        await dialog.ApplyCommand.ExecuteAsync(null);
        var asked = (dialog.IsConfirming, dialog.ConfirmText, writer.Applied.Count);
        await dialog.ConfirmCommand.ExecuteAsync(null);
        await shown.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        asked.ShouldBe((true, TestData.CreateLocalization().GetLocal("FolderSelection.Confirm.Prompt.One", "1"), 0));
        (writer.Applied.Count, dialog.IsOpen).ShouldBe((1, false));
    }

    [Fact]
    public async Task when_the_confirmation_is_backed_out_of_then_nothing_is_saved_and_the_changes_are_kept()
    {
        tree.Levels = _ => Level(Node("Docs"));
        Stored(SelectionRuleFor("Docs", included: true));
        Synced("Docs");
        var dialog = await OpenAsync();
        dialog.Roots.Single().ToggleCommand.Execute(null);
        await dialog.ApplyCommand.ExecuteAsync(null);

        dialog.BackCommand.Execute(null);

        (dialog.IsConfirming, dialog.IsShowingTree, dialog.HasChanges, writer.Applied.Count).ShouldBe((false, true, true, 0));
    }

    [Fact]
    public async Task when_a_folder_that_never_synced_is_unticked_then_apply_saves_without_asking()
    {
        tree.Levels = _ => Level(Node("Docs"));
        Stored(SelectionRuleFor("Docs", included: true));
        var dialog = await OpenAsync();
        dialog.Roots.Single().ToggleCommand.Execute(null);

        await dialog.ApplyCommand.ExecuteAsync(null);

        (dialog.IsConfirming, writer.Applied.Count).ShouldBe((false, 1));
    }

    [Fact]
    public async Task when_the_dialog_is_already_open_then_showing_it_again_does_nothing()
    {
        tree.Levels = _ => Level(Node("Docs"));
        var dialog = await OpenAsync();

        await dialog.ShowAsync(Option.None<AccountId>(), CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        (dialog.IsShowingTree, dialog.IsChoosingAccount).ShouldBe((true, false));
    }

    private static Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure> Level(params FolderTreeNode[] nodes) => Result.Success<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>(nodes);

    private static string Text(string key) => TestData.CreateLocalization().GetLocal(key);

    private static FolderTreeNode Node(string name, string? parent = null, bool children = false, bool clash = false, string? remoteId = null) =>
        FolderTreeNodeFactory.Create(
            name,
            RelativePaths.Of(parent is null ? name : $"{parent}/{name}"),
            remoteId is null ? Option.None<RemoteItemId>() : RemoteItemIdFactory.Create(remoteId).Match(Option.Some, _ => Option.None<RemoteItemId>()),
            FolderOriginFactory.CreateBoth(),
            children ? FolderChildrenFactory.CreateMayHaveChildFolders() : FolderChildrenFactory.CreateNoChildFolders(),
            SelectionStateFactory.CreateUncovered(),
            clash ? FolderNameClashFactory.CreateCaseClash() : FolderNameClashFactory.CreateUnique());

    private static SelectionRule SelectionRuleFor(string path, bool included, SyncDirection? direction = null) =>
        SelectionRuleFactory.Create(Account, RelativePaths.Of(path), Option.None<RemoteItemId>(), included ? SelectionRuleKindFactory.CreateInclude(direction ?? SyncDirectionFactory.CreateTwoWay()) : SelectionRuleKindFactory.CreateExclude());

    private void Stored(params SelectionRule[] stored) => rules.Rules = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>(stored);

    private void Synced(string path)
    {
        var pair = SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), Account, path, RelativePaths.Of(path), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateUpToDate(), 0, Option.Some(TestData.Now));
        pairReader = new FakeSyncPairReader { Pairs = _ => Result.Success<IReadOnlyList<SyncPair>, ErrorResponse>([pair]) };
    }

    private FolderSelectionViewModel Create() => new(tree, rules, writer, new SyncSelectionEvaluator(), pairReader, accounts, TestData.CreateLocalization());

    private static StoredAccount StoredPersonal() =>
        StoredAccountFactory.Create(Account, AccountRegistrationFactory.Create(ProviderAccountKeyFactory.Create("personal-key").Match(key => key, _ => throw new InvalidOperationException("The test key must be valid.")), CloudProviderFactory.CreateOneDrive(), "Personal", "personal@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1, 2)));

    private async Task<FolderSelectionViewModel> OpenAsync()
    {
        var dialog = Create();
        _ = dialog.ShowAsync(Option.Some(Account), CancellationToken);

        return dialog;
    }
}
