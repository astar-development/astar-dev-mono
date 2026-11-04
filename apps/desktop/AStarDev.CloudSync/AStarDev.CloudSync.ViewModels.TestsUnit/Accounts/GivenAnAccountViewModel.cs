using AStarDev.CloudSync.Core.Activity;
using AStarDev.CloudSync.Core.SyncPairs;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.Domain.Activity;
using AStarDev.CloudSync.Domain.Common;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public class GivenAnAccountViewModel
{
    [Fact]
    public void when_created_then_the_counts_are_formatted_for_the_culture()
    {
        var account = new AccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>()), [], TestData.CreateLocalization(), TestData.CreateTime());

        account.FoldersText.ShouldBe("30");
        account.FilesText.ShouldBe("240,686");
    }

    [Fact]
    public void when_the_account_has_never_synced_then_the_last_sync_text_says_never()
    {
        var account = new AccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>()), [], TestData.CreateLocalization(), TestData.CreateTime());

        account.LastSyncText.ShouldBe("Never");
    }

    [Fact]
    public void when_the_account_last_synced_yesterday_then_the_last_sync_text_says_yesterday()
    {
        var account = new AccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.Some(TestData.Now.AddHours(-30))), [], TestData.CreateLocalization(), TestData.CreateTime());

        account.LastSyncText.ShouldBe("Yesterday");
    }

    [Fact]
    public void when_created_then_the_provider_status_and_storage_are_exposed()
    {
        var account = new AccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateSyncing(40), Option.None<DateTimeOffset>()), [], TestData.CreateLocalization(), TestData.CreateTime());

        account.Provider.Name.ShouldBe("OneDrive");
        account.Status.Text.ShouldBe("Syncing 40%");
        account.Storage.Text.ShouldBe("728.9 GB of 1.0 TB");
    }

    [Fact]
    public void when_created_with_activity_then_only_its_own_three_newest_entries_are_shown_newest_first()
    {
        var model = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var otherAccountId = AccountIdFactory.Create(Guid.NewGuid());
        ActivityEntry[] activity =
        [
            ActivityEntryFactory.Create(model.Id, Option.None<SyncPairId>(), ActivityKindFactory.CreateAuthenticated(), TestData.Now.AddMinutes(-10)),
            ActivityEntryFactory.Create(model.Id, Option.None<SyncPairId>(), ActivityKindFactory.CreateDetectingChanges(), TestData.Now.AddMinutes(-1)),
            ActivityEntryFactory.Create(otherAccountId, Option.None<SyncPairId>(), ActivityKindFactory.CreateDownloaded(3), TestData.Now),
            ActivityEntryFactory.Create(model.Id, Option.None<SyncPairId>(), ActivityKindFactory.CreateUploaded(12, 88_080_384), TestData.Now.AddMinutes(-2)),
            ActivityEntryFactory.Create(model.Id, Option.None<SyncPairId>(), ActivityKindFactory.CreateDownloaded(1), TestData.Now.AddMinutes(-30))
        ];

        var account = new AccountViewModel(model, activity, TestData.CreateLocalization(), TestData.CreateTime());

        account.RecentActivity.Select(item => item.Text).ShouldBe(["Detecting local changes…", "Uploaded 12 files (84.0 MB)", "Authenticated"]);
    }
}
