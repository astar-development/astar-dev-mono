using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Activity;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.SyncPairs;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;

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

    private static (AccountDetailViewModel Detail, RecordingSyncActions SyncActions) CreateDetailWithRecordedActions()
    {
        var localization = TestData.CreateLocalization();
        var syncActions = new RecordingSyncActions();
        var account = new AccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>()), [], localization, TestData.CreateTime());

        return (new AccountDetailViewModel(account, [], [], TestData.CreateLocalPathFormatter(), syncActions, localization), syncActions);
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

        var detail = new AccountDetailViewModel(account, [], activity, TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), localization);

        detail.RecentActivity.Select(item => item.Text).ShouldBe(["Downloaded 1 file", "Downloaded 2 files", "Downloaded 3 files", "Downloaded 4 files", "Downloaded 5 files"]);
    }

    [Fact]
    public void when_the_account_has_a_sync_pair_then_its_synced_folder_shows_the_local_path_relative_to_the_sync_root()
    {
        var localization = TestData.CreateLocalization();
        var cloudAccount = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var model = SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), cloudAccount.Id, "Documents", RelativePaths.Of("Documents"), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateUpToDate(), 1, Option.None<DateTimeOffset>());
        var pair = new SyncPairViewModel(model, cloudAccount, localization, TestData.CreateTime());
        var account = new AccountViewModel(cloudAccount, [], localization, TestData.CreateTime());

        var detail = new AccountDetailViewModel(account, [pair], [], TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), localization);

        var folder = detail.SyncedFolders.ShouldHaveSingleItem();
        folder.LocalPathText.ShouldBe("Documents");
        folder.Pair.LocalPath.ShouldBe("Documents");
    }

    [Fact]
    public void when_the_account_needs_sign_in_then_pause_and_sync_now_are_disabled()
    {
        var account = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), NoAccountCommands.Instance);

        var detail = new AccountDetailViewModel(account, [], [], TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), TestData.CreateLocalization());

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

        var detail = new AccountDetailViewModel(account, [], [], TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), localization);

        detail.Stats.Take(2).Select(tile => (tile.Value, tile.Label)).ShouldBe([("\u2014", "folders"), ("\u2014", "files")]);
    }

    private static AccountDetailViewModel CreateDetail(int folderCount, int fileCount, SyncStatus status)
    {
        var localization = TestData.CreateLocalization();
        var cloudAccount = CloudAccountFactory.Create(CloudAccountIdentityFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive()), StorageUsageFactory.Create(1, 2), folderCount, fileCount, status, Option.None<DateTimeOffset>());
        var account = new AccountViewModel(cloudAccount, [], localization, TestData.CreateTime());

        return new AccountDetailViewModel(account, [], [], TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), localization);
    }

    private static ActivityItemViewModel CreateActivity(AccountId accountId, ActivityKind kind, ILocalizationService localization)
    {
        var entry = ActivityEntryFactory.Create(accountId, Option.None<SyncPairId>(), kind, TestData.Now);

        return new ActivityItemViewModel(entry, "Captain Flint", new ProviderViewModel(CloudProviderFactory.CreateOneDrive(), localization), localization, TestData.CreateTime());
    }
}
