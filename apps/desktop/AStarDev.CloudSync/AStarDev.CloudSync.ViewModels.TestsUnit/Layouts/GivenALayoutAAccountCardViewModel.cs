using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Layouts;

public class GivenALayoutAAccountCardViewModel
{
    [Fact]
    public async Task when_the_action_command_runs_while_the_account_is_syncing_then_sync_is_cancelled_for_the_account()
    {
        var syncActions = new RecordingSyncActions();
        var account = new AccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateSyncing(25), Option.None<DateTimeOffset>()), [], TestData.CreateLocalization(), TestData.CreateTime());
        var card = new LayoutAAccountCardViewModel(account, _ => { }, syncActions, TestData.CreateLocalization());

        await card.ActionCommand.ExecuteAsync(null);

        syncActions.Calls.ShouldBe([$"CancelSyncAsync:{account.Id}"]);
    }

    [Fact]
    public async Task when_the_action_command_runs_while_the_account_is_not_syncing_then_the_account_syncs_now()
    {
        var syncActions = new RecordingSyncActions();
        var account = new AccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>()), [], TestData.CreateLocalization(), TestData.CreateTime());
        var card = new LayoutAAccountCardViewModel(account, _ => { }, syncActions, TestData.CreateLocalization());

        await card.ActionCommand.ExecuteAsync(null);

        syncActions.Calls.ShouldBe([$"SyncNowAsync:{account.Id}"]);
    }

    [Fact]
    public void when_the_account_is_syncing_then_the_action_is_cancel()
    {
        var card = CreateCard(SyncStatusFactory.CreateSyncing(25));

        card.ActionText.ShouldBe("Cancel");
    }

    [Theory]
    [MemberData(nameof(NotSyncingStatuses))]
    public void when_the_account_is_not_syncing_then_the_action_is_sync_now(string statusName)
    {
        var card = CreateCard(NotSyncingStatus(statusName));

        card.ActionText.ShouldBe("Sync now");
    }

    [Fact]
    public void when_created_then_the_email_and_provider_are_joined()
    {
        var card = CreateCard(SyncStatusFactory.CreateUpToDate());

        card.EmailAndProviderText.ShouldBe("work@example.com · OneDrive");
    }

    [Fact]
    public void when_created_then_the_card_labels_are_localised()
    {
        var card = CreateCard(SyncStatusFactory.CreateUpToDate());

        card.StorageLabel.ShouldBe("Storage");
        card.FoldersLabel.ShouldBe("folders");
        card.FilesLabel.ShouldBe("files");
        card.ConflictsLabel.ShouldBe("conflicts");
        card.LastSyncLabel.ShouldBe("last sync");
        card.RecentActivityLabel.ShouldBe("Recent activity");
    }

    [Fact]
    public void when_the_account_has_one_folder_file_and_conflict_then_the_count_labels_are_singular()
    {
        var card = CreateCard(1, 1, SyncStatusFactory.CreateHasConflicts(1));

        card.FoldersLabel.ShouldBe("folder");
        card.FilesLabel.ShouldBe("file");
        card.ConflictsLabel.ShouldBe("conflict");
    }

    [Fact]
    public void when_the_account_has_more_than_one_folder_file_and_conflict_then_the_count_labels_are_plural()
    {
        var card = CreateCard(2, 3, SyncStatusFactory.CreateHasConflicts(2));

        card.FoldersLabel.ShouldBe("folders");
        card.FilesLabel.ShouldBe("files");
        card.ConflictsLabel.ShouldBe("conflicts");
    }

    [Fact]
    public void when_created_then_it_is_not_selected()
    {
        var card = CreateCard(SyncStatusFactory.CreateUpToDate());

        card.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void when_the_select_command_runs_then_the_card_is_passed_to_the_select_callback()
    {
        var localization = TestData.CreateLocalization();
        var account = new AccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>()), [], localization, TestData.CreateTime());
        List<LayoutAAccountCardViewModel> selected = [];
        var card = new LayoutAAccountCardViewModel(account, selected.Add, new RecordingSyncActions(), localization);

        card.SelectCommand.Execute(null);

        selected.ShouldHaveSingleItem().ShouldBeSameAs(card);
    }

    public static TheoryData<string> NotSyncingStatuses() => ["up-to-date", "conflicts", "paused"];

    private static SyncStatus NotSyncingStatus(string statusName) => statusName switch
    {
        "conflicts" => SyncStatusFactory.CreateHasConflicts(2),
        "paused" => SyncStatusFactory.CreatePaused(),
        _ => SyncStatusFactory.CreateUpToDate()
    };

    private static LayoutAAccountCardViewModel CreateCard(SyncStatus status)
    {
        var localization = TestData.CreateLocalization();
        var account = new AccountViewModel(TestData.CreateAccount(status, Option.None<DateTimeOffset>()), [], localization, TestData.CreateTime());

        return new LayoutAAccountCardViewModel(account, _ => { }, new RecordingSyncActions(), localization);
    }

    private static LayoutAAccountCardViewModel CreateCard(int folderCount, int fileCount, SyncStatus status)
    {
        var localization = TestData.CreateLocalization();
        var cloudAccount = CloudAccountFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive(), StorageUsageFactory.Create(1, 2), folderCount, fileCount, status, Option.None<DateTimeOffset>());
        var account = new AccountViewModel(cloudAccount, [], localization, TestData.CreateTime());

        return new LayoutAAccountCardViewModel(account, _ => { }, new RecordingSyncActions(), localization);
    }
}
