using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncScheduler
{
    [Fact]
    public void when_constructed_then_scheduler_is_not_null()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();

        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void when_default_interval_accessed_then_returns_60_minutes()
        => SyncScheduler.DefaultInterval.ShouldBe(TimeSpan.FromMinutes(60));

    [Fact]
    public void when_started_then_scheduler_is_not_null()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        scheduler.StartSync();

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void when_started_with_default_interval_then_scheduler_is_not_null()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        scheduler.StartSync();

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void when_started_with_custom_interval_then_scheduler_is_not_null()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var customInterval = TimeSpan.FromMinutes(30);

        scheduler.StartSync(customInterval);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void when_stopped_after_start_then_scheduler_is_not_null()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        scheduler.StartSync();

        scheduler.StopSync();

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void when_interval_set_after_start_then_scheduler_is_not_null()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        scheduler.StartSync();
        var newInterval = TimeSpan.FromMinutes(30);

        scheduler.SetInterval(newInterval);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_trigger_now_then_repository_get_all_called_once()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([new() { Email = "test@example.com", DisplayName = "test" }]);

        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        await scheduler.TriggerNowAsync(TestContext.Current.CancellationToken);

        _ = await mockRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_now_called_then_scheduler_is_not_null()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([new() { Email = "test@example.com", DisplayName = "test" }]);

        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        await scheduler.TriggerNowAsync(TestContext.Current.CancellationToken);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_trigger_account_then_sync_service_called_for_account()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount { Id = "test-account" };

        await scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        await mockSyncService.Received(1).SyncAccountAsync(account, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_account_then_sync_started_event_raised()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount { Id = "test-account" };
        bool eventRaised = false;
        string? raisedAccountId = null;

        scheduler.SyncStarted += (s, accountId) =>
        {
            eventRaised = true;
            raisedAccountId = accountId;
        };

        await scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        eventRaised.ShouldBeTrue();
        raisedAccountId.ShouldBe("test-account");
    }

    [Fact]
    public async Task when_trigger_account_then_sync_completed_event_raised()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount { Id = "test-account" };
        bool eventRaised = false;
        string? raisedAccountId = null;

        scheduler.SyncCompleted += (s, accountId) =>
        {
            eventRaised = true;
            raisedAccountId = accountId;
        };

        await scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        eventRaised.ShouldBeTrue();
        raisedAccountId.ShouldBe("test-account");
    }

    [Fact]
    public async Task when_trigger_account_and_sync_service_throws_then_completed_event_still_raised()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount { Id = "test-account" };

        _ = mockSyncService.SyncAccountAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Sync failed")));

        bool completedEventRaised = false;
        scheduler.SyncCompleted += (s, accountId) => completedEventRaised = true;

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await scheduler.TriggerAccountAsync(account));

        completedEventRaised.ShouldBeTrue();
    }

    [Fact]
    public void when_scheduler_created_then_it_is_async_disposable()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        _ = scheduler.ShouldBeAssignableTo<IAsyncDisposable>();
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public void when_started_with_various_intervals_then_scheduler_is_not_null(int minutes)
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var interval = TimeSpan.FromMinutes(minutes);

        scheduler.StartSync(interval);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_trigger_account_then_correct_account_data_passed_to_sync_service()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount
        {
            Id = "account-123",
            Email = "test@outlook.com",
            DisplayName = "Test User"
        };

        await scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        await mockSyncService.Received(1).SyncAccountAsync(
            Arg.Is<OneDriveAccount>(a => a.Id == "account-123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_account_by_id_and_account_exists_then_sync_service_called()
    {
        const string accountId = "account-456";
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetByIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(new AccountEntity
        {
            Id = accountId,
            DisplayName = "Test User",
            Email = "test@outlook.com",
            LocalSyncPath = "/some/path",
            ConflictPolicy = ConflictPolicy.Ignore,
            SyncFolders = []
        });
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        await scheduler.TriggerAccountAsync(accountId, TestContext.Current.CancellationToken);

        await mockSyncService.Received(1).SyncAccountAsync(
            Arg.Is<OneDriveAccount>(a => a.Id == accountId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_account_by_id_and_account_not_found_then_sync_service_not_called()
    {
        const string accountId = "missing-account";
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetByIdAsync(accountId, Arg.Any<CancellationToken>()).Returns((AccountEntity?)null);
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        await scheduler.TriggerAccountAsync(accountId, TestContext.Current.CancellationToken);

        await mockSyncService.DidNotReceive().SyncAccountAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_account_by_id_and_account_exists_then_sync_started_event_raised()
    {
        const string accountId = "account-789";
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetByIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(new AccountEntity { Id = accountId, DisplayName = "Test", Email = "test@test.com", SyncFolders = [] });
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        string? raisedId = null;
        scheduler.SyncStarted += (_, id) => raisedId = id;

        await scheduler.TriggerAccountAsync(accountId, TestContext.Current.CancellationToken);

        raisedId.ShouldBe(accountId);
    }

    [Fact]
    public async Task when_trigger_account_by_id_and_account_exists_then_sync_completed_event_raised()
    {
        const string accountId = "account-789";
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetByIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(new AccountEntity { Id = accountId, DisplayName = "Test", Email = "test@test.com", SyncFolders = [] });
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        string? raisedId = null;
        scheduler.SyncCompleted += (_, id) => raisedId = id;

        await scheduler.TriggerAccountAsync(accountId, TestContext.Current.CancellationToken);

        raisedId.ShouldBe(accountId);
    }
}
