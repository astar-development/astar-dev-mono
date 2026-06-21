using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Dashboard;

public sealed class GivenADashboardViewModelWithQuotaUpdate
{
    private readonly ISyncScheduler _scheduler = Substitute.For<ISyncScheduler>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();

    private DashboardViewModel CreateSut() => new(_localizationService, _syncEventAggregator, new DashboardAccountViewModelFactory(_scheduler, _accountRepository, _localizationService, new ActivityItemViewModelFactory(_localizationService)), new ActivityItemViewModelFactory(_localizationService), Substitute.For<IUiTimer>());

    private static OneDriveAccount BuildAccount(string id) => new() { Id = new AccountId(id) };

    [Fact]
    public void when_update_quota_called_for_known_account_then_quota_total_is_updated()
    {
        var account = BuildAccount("acc-1");
        var sut = CreateSut();
        sut.AddAccount(account);

        sut.UpdateQuota("acc-1", StorageQuotaFactory.Create(1_073_741_824L, 536_870_912L));

        sut.AccountSections.Single().QuotaTotal.ShouldBe(1_073_741_824L);
    }

    [Fact]
    public void when_update_quota_called_for_known_account_then_quota_used_is_updated()
    {
        var account = BuildAccount("acc-1");
        var sut = CreateSut();
        sut.AddAccount(account);

        sut.UpdateQuota("acc-1", StorageQuotaFactory.Create(1_073_741_824L, 536_870_912L));

        sut.AccountSections.Single().QuotaUsed.ShouldBe(536_870_912L);
    }

    [Fact]
    public void when_update_quota_called_for_known_account_then_storage_fraction_is_updated()
    {
        var account = BuildAccount("acc-1");
        var sut = CreateSut();
        sut.AddAccount(account);

        sut.UpdateQuota("acc-1", StorageQuotaFactory.Create(1_073_741_824L, 536_870_912L));

        sut.AccountSections.Single().StorageFraction.ShouldBe(0.5);
    }

    [Fact]
    public void when_update_quota_called_for_unknown_account_then_no_exception_is_thrown()
    {
        var sut = CreateSut();

        var exception = Record.Exception(() => sut.UpdateQuota("does-not-exist", StorageQuotaFactory.Create(100L, 50L)));

        exception.ShouldBeNull();
    }
}
