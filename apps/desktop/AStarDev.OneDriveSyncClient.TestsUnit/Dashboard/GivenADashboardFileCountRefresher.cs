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

public sealed class GivenADashboardFileCountRefresher
{
    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();
    private readonly DashboardViewModel _dashboard;
    private readonly DashboardFileCountRefresher _sut;

    public GivenADashboardFileCountRefresher()
    {
        _dashboard = new DashboardViewModel(_localizationService, _syncEventAggregator, new DashboardAccountViewModelFactory(Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), _localizationService, new ActivityItemViewModelFactory(_localizationService), Substitute.For<ILogger<DashboardAccountViewModel>>()), new ActivityItemViewModelFactory(_localizationService), Substitute.For<IUiTimer>());
        _sut = new DashboardFileCountRefresher(_syncedItemRepository, _syncEventAggregator, _dashboard, Substitute.For<ILogger<DashboardFileCountRefresher>>());
        _dashboard.AddAccount(Account);
    }

    private static OneDriveAccount Account => new() { Id = new AccountId("acc-1") };

    [Fact]
    public async Task when_refresh_is_called_then_dashboard_shows_the_repository_file_count()
    {
        _syncedItemRepository.CountFilesAsync(new AccountId("acc-1"), Arg.Any<CancellationToken>()).Returns(7);

        await _sut.RefreshAsync(Account, TestContext.Current.CancellationToken);

        _dashboard.AccountSections.Single().FileCount.ShouldBe(7);
    }

    [Fact]
    public async Task when_repository_throws_then_previous_file_count_is_kept()
    {
        _dashboard.UpdateFileCount("acc-1", 3);
        _syncedItemRepository.CountFilesAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns<int>(_ => throw new InvalidOperationException("boom"));

        await _sut.RefreshAsync(Account, TestContext.Current.CancellationToken);

        _dashboard.AccountSections.Single().FileCount.ShouldBe(3);
    }

    [Fact]
    public async Task when_sync_completes_after_subscribing_then_dashboard_file_count_is_refreshed()
    {
        _syncedItemRepository.CountFilesAsync(new AccountId("acc-1"), Arg.Any<CancellationToken>()).Returns(9);
        _sut.SubscribeToSyncEvents();

        _syncEventAggregator.SyncCompleted += Raise.Event<EventHandler<string>>(this, "acc-1");
        await Task.Delay(200, TestContext.Current.CancellationToken);

        _dashboard.AccountSections.Single().FileCount.ShouldBe(9);
    }
}
