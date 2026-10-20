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

public sealed class GivenADashboardViewModelShowingElapsedTime
{
    private readonly ISyncScheduler scheduler = Substitute.For<ISyncScheduler>();
    private readonly ISyncEventAggregator syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly IAccountRepository accountRepository = Substitute.For<IAccountRepository>();
    private readonly ILocalizationService localizationService = Substitute.For<ILocalizationService>();

    private DashboardViewModel CreateSut()
    {
        var sut = new DashboardViewModel(localizationService, syncEventAggregator, new DashboardAccountViewModelFactory(scheduler, accountRepository, localizationService, new ActivityItemViewModelFactory(localizationService), Substitute.For<ILogger<DashboardAccountViewModel>>()), new ActivityItemViewModelFactory(localizationService), Substitute.For<IUiTimer>());
        sut.SubscribeToSyncEvents();
        sut.AddAccount(new OneDriveAccount { Id = new AccountId("acc-1") });

        return sut;
    }

    [Fact]
    public void when_a_stage_message_arrives_while_syncing_then_the_activity_item_shows_the_elapsed_time()
    {
        var sut = CreateSut();

        syncEventAggregator.SyncProgressChanged += Raise.Event<EventHandler<SyncProgressEventArgs>>(this, new SyncProgressEventArgs("acc-1", "Enumerating", SyncState.Syncing));

        sut.AccountSections[0].RecentActivity[0].ElapsedText.ShouldBe("00:00:00");
    }

    [Fact]
    public void when_the_sync_has_not_started_then_the_activity_item_shows_no_elapsed_time()
    {
        var sut = CreateSut();

        syncEventAggregator.SyncProgressChanged += Raise.Event<EventHandler<SyncProgressEventArgs>>(this, new SyncProgressEventArgs("acc-1", "Idle message", SyncState.Idle));

        sut.AccountSections[0].RecentActivity[0].ElapsedText.ShouldBeEmpty();
    }
}
