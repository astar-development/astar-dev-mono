using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Dashboard;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Dashboard;

public sealed class GivenADashboardViewModelUpdatingFolderCount
{
    private readonly ISyncScheduler _scheduler = Substitute.For<ISyncScheduler>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();

    private DashboardViewModel CreateSut() => new(_localizationService, _syncEventAggregator, new DashboardAccountViewModelFactory(_scheduler, _accountRepository, _localizationService, new ActivityItemViewModelFactory(_localizationService), Substitute.For<ILogger<DashboardAccountViewModel>>()), new ActivityItemViewModelFactory(_localizationService), Substitute.For<IUiTimer>());

    private static OneDriveAccount CreateAccount(string id) => new() { Id = new AccountId(id) };

    [Fact]
    public void when_update_folder_count_called_for_known_account_then_total_folders_reflects_new_count()
    {
        var sut = CreateSut();
        sut.AddAccount(CreateAccount("acc-1"));

        sut.UpdateFolderCount("acc-1", 5);

        sut.TotalFolders.ShouldBe(5);
    }

    [Fact]
    public void when_update_folder_count_called_for_unknown_account_then_total_folders_is_unchanged()
    {
        var sut = CreateSut();
        sut.AddAccount(CreateAccount("acc-1"));

        sut.UpdateFolderCount("acc-unknown", 5);

        sut.TotalFolders.ShouldBe(0);
    }

    [Fact]
    public void when_update_folder_count_called_for_one_of_two_accounts_then_total_folders_is_sum_of_both()
    {
        var account1 = new OneDriveAccount { Id = new AccountId("acc-1"), SelectedFolderIds = [new OneDriveFolderId("f1"), new OneDriveFolderId("f2")] };
        var sut = CreateSut();
        sut.AddAccount(account1);
        sut.AddAccount(CreateAccount("acc-2"));

        sut.UpdateFolderCount("acc-2", 3);

        sut.TotalFolders.ShouldBe(5);
    }
}
