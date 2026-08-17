using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Dashboard;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Dashboard;

public sealed class GivenADashboardAccountViewModelFactory
{
    private static DashboardAccountViewModelFactory CreateSut() => new(Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), Substitute.For<ILocalizationService>(), Substitute.For<IActivityItemViewModelFactory>(), Substitute.For<ILogger<DashboardAccountViewModel>>());

    [Fact]
    public void when_create_is_called_then_the_section_targets_the_account()
    {
        var sut = CreateSut();
        var account = new OneDriveAccount { Id = new AccountId("account-1") };

        var section = sut.Create(account);

        section.AccountId.ShouldBe("account-1");
    }

    [Fact]
    public void when_create_is_called_then_the_folder_count_matches_the_account_selection()
    {
        var sut = CreateSut();
        var account = new OneDriveAccount { Id = new AccountId("account-1"), SelectedFolderIds = [new OneDriveFolderId("folder-1"), new OneDriveFolderId("folder-2")] };

        var section = sut.Create(account);

        section.FolderCount.ShouldBe(2);
    }
}
