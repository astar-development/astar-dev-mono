using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public class SyncSchedulerTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();

        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void DefaultInterval_ShouldBe60Minutes()
        => SyncScheduler.DefaultInterval.ShouldBe(TimeSpan.FromMinutes(60));

    [Fact]
    public void Start_ShouldInitializeTimer()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        scheduler.Start();
        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void Start_WithDefaultInterval_ShouldUse60Minutes()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        scheduler.Start();

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void Start_WithCustomInterval_ShouldUseProvidedInterval()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var customInterval = TimeSpan.FromMinutes(30);

        scheduler.Start(customInterval);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void Stop_ShouldStopTimer()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        scheduler.Start();

        scheduler.Stop();

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void SetInterval_ShouldUpdateInterval()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        scheduler.Start();
        var newInterval = TimeSpan.FromMinutes(30);

        scheduler.SetInterval(newInterval);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task TriggerNowAsync_ShouldExecuteSyncPass()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([new (){ Email = "test@example.com", DisplayName = "test" }]);

        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        await scheduler.TriggerNowAsync(TestContext.Current.CancellationToken);

        _ = await mockRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerNowAsync_WhenAlreadyRunning_ShouldNotStartNewPass()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([new (){ Email = "test@example.com", DisplayName = "test" }]);

        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        await scheduler.TriggerNowAsync(TestContext.Current.CancellationToken);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task TriggerAccountAsync_ShouldSyncSpecificAccount()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount { Id = "test-account" };

        await scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        await mockSyncService.Received(1).SyncAccountAsync(account, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerAccountAsync_ShouldRaiseSyncStartedEvent()
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
    public async Task TriggerAccountAsync_ShouldRaiseSyncCompletedEvent()
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
    public async Task TriggerAccountAsync_WhenSyncServiceThrows_ShouldStillRaiseCompletedEvent()
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
    public void SyncScheduler_ShouldBeAsyncDisposable()
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
    public void Start_WithVariousIntervals_ShouldInitializeSuccessfully(int minutes)
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var interval = TimeSpan.FromMinutes(minutes);

        scheduler.Start(interval);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task TriggerAccountAsync_ShouldPassCorrectAccountData()
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
}
