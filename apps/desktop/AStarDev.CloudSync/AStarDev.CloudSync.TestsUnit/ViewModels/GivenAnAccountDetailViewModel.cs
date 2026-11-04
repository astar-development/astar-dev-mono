using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.ViewModels;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenAnAccountDetailViewModel
{
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

    private static AccountDetailViewModel CreateDetail(int folderCount, int fileCount, SyncStatus status)
    {
        var localization = TestData.CreateLocalization();
        var cloudAccount = CloudAccountFactory.Create(AccountIdFactory.Create(), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive(), StorageUsageFactory.Create(1, 2), folderCount, fileCount, status, Option.None<DateTimeOffset>());
        var account = new AccountViewModel(cloudAccount, [], localization, TestData.CreateTime());

        return new AccountDetailViewModel(account, [], localization);
    }
}
