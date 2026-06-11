using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Dashboard;

public sealed class GivenADashboardViewModelTrackingConflicts
{
    private readonly ISyncScheduler _scheduler = Substitute.For<ISyncScheduler>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();

    private DashboardViewModel CreateSut() => new(_localizationService, _syncEventAggregator, new DashboardAccountViewModelFactory(_scheduler, _accountRepository, _localizationService, new ActivityItemViewModelFactory(_localizationService)), new ActivityItemViewModelFactory(_localizationService));

    private static OneDriveAccount CreateAccount(string id) => new() { Id = new AccountId(id) };

    private static SyncConflict CreateConflict(string accountId) => new()
    {
        Id       = Guid.NewGuid(),
        Remote   = RemoteItemRefFactory.Create(new AccountId(accountId), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty)),
        Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow, 0L, DateTimeOffset.UtcNow.AddMinutes(-5), 0L),
        State    = ConflictState.Pending
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
