using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.ViewModels;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenASyncPairViewModel
{
    [Fact]
    public void when_created_then_the_folders_account_and_direction_are_shown()
    {
        var pair = CreatePair(SyncDirectionFactory.CreateUploadOnly(), SyncStatusFactory.CreateUpToDate());

        pair.Name.ShouldBe("Documents");
        pair.LocalPath.ShouldBe("/home/user/CloudSync/OneDrive/Work/Documents");
        pair.RemotePath.ShouldBe("/Documents");
        pair.AccountName.ShouldBe("Work");
        pair.DirectionLabel.ShouldBe("Upload only");
        pair.DirectionGlyph.ShouldBe("→");
    }

    [Fact]
    public void when_syncing_then_the_progress_is_the_percentage_as_a_fraction()
    {
        var pair = CreatePair(SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateSyncing(25));

        pair.Progress.ShouldBe(0.25);
    }

    [Fact]
    public void when_not_syncing_then_the_progress_is_complete()
    {
        var pair = CreatePair(SyncDirectionFactory.CreateDownloadOnly(), SyncStatusFactory.CreateUpToDate());

        pair.Progress.ShouldBe(1);
        pair.DirectionLabel.ShouldBe("Download only");
    }

    [Fact]
    public void when_created_then_the_file_count_and_last_sync_are_summarised()
    {
        var pair = CreatePair(SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateUpToDate());

        pair.SummaryText.ShouldBe("1,118 files · 4 min ago");
    }

    [Fact]
    public void when_created_then_the_last_sync_and_account_id_are_exposed()
    {
        var account = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var model = SyncPairFactory.Create(SyncPairIdFactory.Create(), account.Id, "Documents", new DirectoryInfo("/home/user/CloudSync/OneDrive/Work/Documents"), "/Documents", SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateUpToDate(), 1_118, Option.Some(TestData.Now.AddMinutes(-4)));

        var pair = new SyncPairViewModel(model, account, TestData.CreateLocalization(), TestData.CreateTime());

        pair.LastSyncText.ShouldBe("4 min ago");
        pair.AccountId.ShouldBe(account.Id);
    }

    private static SyncPairViewModel CreatePair(SyncDirection direction, SyncStatus status)
    {
        var account = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var model = SyncPairFactory.Create(SyncPairIdFactory.Create(), account.Id, "Documents", new DirectoryInfo("/home/user/CloudSync/OneDrive/Work/Documents"), "/Documents", direction, status, 1_118, Option.Some(TestData.Now.AddMinutes(-4)));
        var localization = TestData.CreateLocalization();

        return new SyncPairViewModel(model, account, localization, TestData.CreateTime());
    }
}
