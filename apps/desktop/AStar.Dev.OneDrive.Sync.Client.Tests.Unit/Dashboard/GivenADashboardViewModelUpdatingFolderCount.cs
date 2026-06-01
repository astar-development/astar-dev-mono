using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Dashboard;

public sealed class GivenADashboardViewModelUpdatingFolderCount
{
    private readonly ISyncScheduler _scheduler = Substitute.For<ISyncScheduler>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();

    private DashboardViewModel CreateSut() => new(_scheduler, _localizationService, _accountRepository, _syncEventAggregator);

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
