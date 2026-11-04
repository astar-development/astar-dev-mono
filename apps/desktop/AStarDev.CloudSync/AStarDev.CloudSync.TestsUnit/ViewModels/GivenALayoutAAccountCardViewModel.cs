using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.ViewModels;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenALayoutAAccountCardViewModel
{
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

        return new LayoutAAccountCardViewModel(account, localization);
    }
}
