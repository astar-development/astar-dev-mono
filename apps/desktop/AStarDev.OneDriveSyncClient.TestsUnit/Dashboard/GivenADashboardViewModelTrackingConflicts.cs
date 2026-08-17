using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Dashboard;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Dashboard;

public sealed class GivenADashboardViewModelTrackingConflicts
{
    private readonly ISyncScheduler _scheduler = Substitute.For<ISyncScheduler>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();

    private DashboardViewModel CreateSut() => new(_localizationService, _syncEventAggregator, new DashboardAccountViewModelFactory(_scheduler, _accountRepository, _localizationService, new ActivityItemViewModelFactory(_localizationService), Substitute.For<ILogger<DashboardAccountViewModel>>()), new ActivityItemViewModelFactory(_localizationService), Substitute.For<IUiTimer>());

    private static OneDriveAccount CreateAccount(string id) => new() { Id = new AccountId(id) };

    private static SyncConflict CreateConflict(string accountId) => new()
    {
        Id = Guid.NewGuid(),
        Remote = RemoteItemRefFactory.Create(new AccountId(accountId), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty)),
        Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow, 0L, DateTimeOffset.UtcNow.AddMinutes(-5), 0L),
        State = ConflictState.Pending
    };

    [Fact]
    public void when_conflict_resolved_then_total_conflicts_decrements_to_zero()
    {
        var sut = CreateSut();
        sut.SubscribeToSyncEvents();
        sut.AddAccount(CreateAccount("acc-1"));
        var conflict = CreateConflict("acc-1");

        _syncEventAggregator.ConflictDetected += Raise.Event<EventHandler<SyncConflict>>(this, conflict);
        _syncEventAggregator.ConflictResolved += Raise.Event<EventHandler<SyncConflict>>(this, conflict);

        sut.TotalConflicts.ShouldBe(0);
    }

    [Fact]
    public void when_one_of_two_conflicts_resolved_then_total_conflicts_is_one()
    {
        var sut = CreateSut();
        sut.SubscribeToSyncEvents();
        sut.AddAccount(CreateAccount("acc-1"));
        var conflictA = CreateConflict("acc-1");
        var conflictB = CreateConflict("acc-1");

        _syncEventAggregator.ConflictDetected += Raise.Event<EventHandler<SyncConflict>>(this, conflictA);
        _syncEventAggregator.ConflictDetected += Raise.Event<EventHandler<SyncConflict>>(this, conflictB);
        _syncEventAggregator.ConflictResolved += Raise.Event<EventHandler<SyncConflict>>(this, conflictA);

        sut.TotalConflicts.ShouldBe(1);
    }
}
