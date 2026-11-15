using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Conflicts;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Activity;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.CloudSync.ViewModels.SyncPairs;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Layouts;

public class GivenAnAccountDetailViewModel
{
    [Fact]
    public async Task when_the_pause_command_runs_then_the_account_is_paused()
    {
        var (detail, syncActions) = CreateDetailWithRecordedActions();

        await detail.PauseCommand.ExecuteAsync(null);

        syncActions.Calls.ShouldBe([$"PauseAsync:{detail.Account.Id}"]);
    }

    [Fact]
    public async Task when_the_sync_now_command_runs_then_the_account_syncs_now()
    {
        var (detail, syncActions) = CreateDetailWithRecordedActions();

        await detail.SyncNowCommand.ExecuteAsync(null);

        syncActions.Calls.ShouldBe([$"SyncNowAsync:{detail.Account.Id}"]);
    }

    [Fact]
    public async Task when_the_add_folder_command_runs_then_a_sync_pair_is_added_for_the_account()
    {
        var (detail, syncActions) = CreateDetailWithRecordedActions();

        await detail.AddFolderCommand.ExecuteAsync(null);

        syncActions.Calls.ShouldBe([$"AddSyncPairAsync:{detail.Account.Id}"]);
    }

    [Fact]
    public void when_created_then_the_overview_tab_is_selected_and_the_conflicts_are_not_read()
    {
        var reader = new FakeAttentionReader();

        var detail = CreateDetailWithConflicts(reader);

        detail.SelectedTabIndex.ShouldBe(0);
        reader.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_conflicts_tab_is_selected_then_the_accounts_conflicts_are_read()
    {
        var reader = new FakeAttentionReader();
        var detail = CreateDetailWithConflicts(reader);
        reader.Add(detail.Account.Id, AttentionItems.CreateConflict(detail.Account.Id, "Docs/a.docx"));

        detail.SelectedTabIndex = AccountDetailViewModel.ConflictsTabIndex;
        await detail.Conflicts.RefreshCommand.ExecutionTask!;

        detail.Conflicts.Groups.Select(group => group.HeaderText).ShouldBe(["Work · 1 conflict"]);
    }

    [Fact]
    public void when_another_tab_is_selected_then_the_conflicts_are_not_read()
    {
        var reader = new FakeAttentionReader();
        var detail = CreateDetailWithConflicts(reader);

        detail.SelectedTabIndex = 2;

        (detail.SelectedTabIndex, reader.Reads).ShouldBe((2, 0));
    }

    [Fact]
    public async Task when_the_folders_tab_is_selected_then_the_accounts_folders_are_read()
    {
        var tree = new FakeFolderTreeSource();
        var detail = CreateDetail(new FakeAttentionReader(), tree);

        detail.SelectedTabIndex = AccountDetailViewModel.FoldersTabIndex;
        await detail.Folders.ShowAccountCommand.ExecutionTask!;

        tree.RequestedAccounts.ShouldBe([detail.Account.Id]);
    }

    [Fact]
    public void when_another_tab_is_selected_then_the_folders_are_not_read()
    {
        var tree = new FakeFolderTreeSource();
        var detail = CreateDetail(new FakeAttentionReader(), tree);

        detail.SelectedTabIndex = AccountDetailViewModel.ConflictsTabIndex;

        tree.RequestedAccounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_activity_tab_is_selected_then_the_accounts_activity_is_read()
    {
        var activityReader = new FakeActivityLogReader();
        var detail = CreateDetail(new FakeAttentionReader(), new FakeFolderTreeSource(), activityReader);

        detail.SelectedTabIndex = AccountDetailViewModel.ActivityTabIndex;
        await detail.Activity.RefreshCommand.ExecutionTask!;

        activityReader.Requests.ShouldBe([(Option.Some(detail.Account.Id), ActivityLogLimits.EntriesPerAccount)]);
    }

    [Fact]
    public void when_the_overview_tab_is_selected_then_the_activity_is_not_read()
    {
        var activityReader = new FakeActivityLogReader();
        var detail = CreateDetail(new FakeAttentionReader(), new FakeFolderTreeSource(), activityReader);

        detail.SelectedTabIndex = AccountDetailViewModel.FoldersTabIndex;

        activityReader.Requests.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_account_is_paused_then_the_pause_action_reads_resume_and_otherwise_pause()
    {
        var running = CreateDetailWithStatus(SyncStatusFactory.CreateUpToDate());
        var paused = CreateDetailWithStatus(SyncStatusFactory.CreatePaused());

        (running.PauseText, paused.PauseText).ShouldBe(("Pause", "Resume"));
    }

    private static AccountDetailViewModel CreateDetailWithStatus(SyncStatus status)
    {
        var localization = TestData.CreateLocalization();
        var account = new AccountViewModel(TestData.CreateAccount(status, Option.None<DateTimeOffset>()), [], localization, TestData.CreateTime());

        return new AccountDetailViewModel(account, [], [], TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), TestData.CreateAccountDetailTabs(), localization);
    }

    private static AccountDetailViewModel CreateDetailWithConflicts(FakeAttentionReader reader) => CreateDetail(reader, new FakeFolderTreeSource());

    private static AccountDetailViewModel CreateDetail(FakeAttentionReader reader, FakeFolderTreeSource tree) => CreateDetail(reader, tree, new FakeActivityLogReader());

    private static AccountDetailViewModel CreateDetail(FakeAttentionReader reader, FakeFolderTreeSource tree, FakeActivityLogReader activityReader)
    {
        var localization = TestData.CreateLocalization();
        var cloudAccount = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var account = new AccountViewModel(cloudAccount, [], localization, TestData.CreateTime());
        var conflicts = TestData.CreateConflictList(new FixedAccountsSource(cloudAccount), reader, new FakeConflictResolver());

        return new AccountDetailViewModel(account, [], [], TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), AccountDetailTabsFactory.Create(conflicts, TestData.CreateFolderSelectionFactory(tree).CreatePage(), TestData.CreateActivityListFactory(activityReader).Create(new SyncOverviewViewModel(new FixedAccountsSource(cloudAccount), localization, TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance), Option.Some(account.Id))), localization);
    }

    private static (AccountDetailViewModel Detail, RecordingSyncActions SyncActions) CreateDetailWithRecordedActions()
    {
        var localization = TestData.CreateLocalization();
        var syncActions = new RecordingSyncActions();
        var account = new AccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>()), [], localization, TestData.CreateTime());

        return (new AccountDetailViewModel(account, [], [], TestData.CreateLocalPathFormatter(), syncActions, TestData.CreateAccountDetailTabs(), localization), syncActions);
    }

    [Fact]
    public void when_the_account_has_one_folder_file_and_conflict_then_the_stat_labels_are_singular()
    {
        var detail = CreateDetail(1, 1, SyncStatusFactory.CreateHasConflicts(1));

        detail.Stats.Select(tile => tile.Label).ShouldBe(["folder", "file", "conflict", "last sync"]);
    }

    [Fact]
    public void when_the_account_has_more_than_one_folder_file_and_conflict_then_the_stat_labels_are_plural()
    {
        var detail = CreateDetail(2, 3, SyncStatusFactory.CreateHasConflicts(2));

        detail.Stats.Select(tile => tile.Label).ShouldBe(["folders", "files", "conflicts", "last sync"]);
    }

    [Fact]
    public void when_the_account_has_more_than_five_activity_entries_then_only_its_five_newest_are_kept()
    {
        var localization = TestData.CreateLocalization();
        var cloudAccount = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var otherAccountId = AccountIdFactory.Create(Guid.NewGuid());
        ActivityItemViewModel[] activity =
        [
            CreateActivity(cloudAccount.Id, ActivityKindFactory.CreateDownloaded(1), localization),
            CreateActivity(otherAccountId, ActivityKindFactory.CreateAuthenticated(), localization),
            CreateActivity(cloudAccount.Id, ActivityKindFactory.CreateDownloaded(2), localization),
            CreateActivity(cloudAccount.Id, ActivityKindFactory.CreateDownloaded(3), localization),
            CreateActivity(cloudAccount.Id, ActivityKindFactory.CreateDownloaded(4), localization),
            CreateActivity(otherAccountId, ActivityKindFactory.CreateDetectingChanges(), localization),
            CreateActivity(cloudAccount.Id, ActivityKindFactory.CreateDownloaded(5), localization),
            CreateActivity(cloudAccount.Id, ActivityKindFactory.CreateDownloaded(6), localization)
        ];
        var account = new AccountViewModel(cloudAccount, [], localization, TestData.CreateTime());

        var detail = new AccountDetailViewModel(account, [], activity, TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), TestData.CreateAccountDetailTabs(), localization);

        detail.RecentActivity.Select(item => item.Text).ShouldBe(["Downloaded 1 file", "Downloaded 2 files", "Downloaded 3 files", "Downloaded 4 files", "Downloaded 5 files"]);
    }

    [Fact]
    public void when_the_account_has_a_sync_pair_then_its_synced_folder_shows_the_local_path_relative_to_the_sync_root()
    {
        var localization = TestData.CreateLocalization();
        var cloudAccount = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var model = SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), cloudAccount.Id, "Documents", RelativePaths.Of("Documents"), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateTwoWay(), SyncPairStateFactory.Create(SyncStatusFactory.CreateUpToDate(), 1, Option.None<DateTimeOffset>()));
        var pair = new SyncPairViewModel(model, cloudAccount, localization, TestData.CreateTime());
        var account = new AccountViewModel(cloudAccount, [], localization, TestData.CreateTime());

        var detail = new AccountDetailViewModel(account, [pair], [], TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), TestData.CreateAccountDetailTabs(), localization);

        var folder = detail.SyncedFolders.ShouldHaveSingleItem();
        folder.LocalPathText.ShouldBe("Documents");
        folder.Pair.LocalPath.ShouldBe("Documents");
    }

    [Fact]
    public void when_the_account_needs_sign_in_then_pause_and_sync_now_are_disabled()
    {
        var account = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), NoAccountCommands.Instance);

        var detail = new AccountDetailViewModel(account, [], [], TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), TestData.CreateAccountDetailTabs(), TestData.CreateLocalization());

        detail.CanSync.ShouldBeFalse();
    }

    [Fact]
    public void when_the_account_is_signed_in_then_pause_and_sync_now_are_enabled()
    {
        var detail = CreateDetail(1, 1, SyncStatusFactory.CreateUpToDate());

        detail.CanSync.ShouldBeTrue();
    }

    [Fact]
    public void when_the_counts_are_unknown_then_the_folder_and_file_tiles_show_an_em_dash_with_plural_labels()
    {
        var localization = TestData.CreateLocalization();
        var account = new AccountViewModel(TestData.CreateAccountWithUnknownCounts(), [], localization, TestData.CreateTime());

        var detail = new AccountDetailViewModel(account, [], [], TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), TestData.CreateAccountDetailTabs(), localization);

        detail.Stats.Take(2).Select(tile => (tile.Value, tile.Label)).ShouldBe([("\u2014", "folders"), ("\u2014", "files")]);
    }

    private static AccountDetailViewModel CreateDetail(int folderCount, int fileCount, SyncStatus status)
    {
        var localization = TestData.CreateLocalization();
        var cloudAccount = CloudAccountFactory.Create(CloudAccountIdentityFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive()), StorageUsageFactory.Create(1, 2), folderCount, fileCount, status, Option.None<DateTimeOffset>());
        var account = new AccountViewModel(cloudAccount, [], localization, TestData.CreateTime());

        return new AccountDetailViewModel(account, [], [], TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), TestData.CreateAccountDetailTabs(), localization);
    }

    private static ActivityItemViewModel CreateActivity(AccountId accountId, ActivityKind kind, ILocalizationService localization)
    {
        var entry = ActivityEntryFactory.Create(accountId, Option.None<SyncPairId>(), kind, TestData.Now);

        return new ActivityItemViewModel(entry, "Captain Flint", new ProviderViewModel(CloudProviderFactory.CreateOneDrive(), localization), localization, TestData.CreateTime());
    }
}
