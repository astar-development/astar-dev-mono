using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncScheduler
{
    private static SyncScheduler CreateScheduler(ISyncService syncService, IAccountRepository repository, ISyncRuleRepository syncRuleRepository)
        => new(syncService, repository, syncRuleRepository, Substitute.For<ILogger<SyncScheduler>>());

    [Fact]
    public void when_constructed_then_scheduler_is_not_null()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();

        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());

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
        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());

        scheduler.StartSync();

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void when_started_with_default_interval_then_scheduler_is_not_null()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());

        scheduler.StartSync();

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void when_started_with_custom_interval_then_scheduler_is_not_null()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());
        var customInterval = TimeSpan.FromMinutes(30);

        scheduler.StartSync(customInterval);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void when_stopped_after_start_then_scheduler_is_not_null()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());
        scheduler.StartSync();

        scheduler.StopSync();

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void when_interval_set_after_start_then_scheduler_is_not_null()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());
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
        _ = mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([new() { Profile = AccountProfileFactory.Create("test", "test@example.com") }]);

        var scheduler = CreateScheduler(mockSyncService, mockRepository, BuildSyncRuleRepository());

        await scheduler.TriggerNowAsync(TestContext.Current.CancellationToken);

        _ = await mockRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_now_called_then_scheduler_is_not_null()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([new() { Profile = AccountProfileFactory.Create("test", "test@example.com") }]);

        var scheduler = CreateScheduler(mockSyncService, mockRepository, BuildSyncRuleRepository());

        await scheduler.TriggerNowAsync(TestContext.Current.CancellationToken);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_trigger_account_then_sync_service_called_for_account()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());
        var account = new OneDriveAccount { Id = new AccountId("test-account") };

        await scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        await mockSyncService.Received(1).SyncAccountAsync(account, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_account_then_sync_started_event_raised()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());
        var account = new OneDriveAccount { Id = new AccountId("test-account") };
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
        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());
        var account = new OneDriveAccount { Id = new AccountId("test-account") };
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
        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());
        var account = new OneDriveAccount { Id = new AccountId("test-account") };

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
        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());

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
        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());
        var interval = TimeSpan.FromMinutes(minutes);

        scheduler.StartSync(interval);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_trigger_account_then_correct_account_data_passed_to_sync_service()
    {
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());
        var account = new OneDriveAccount
        {
            Id = new AccountId("account-123"),
            Profile = AccountProfileFactory.Create("Test User", "test@outlook.com")
        };

        await scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        await mockSyncService.Received(1).SyncAccountAsync(
            Arg.Is<OneDriveAccount>(a => a.Id == new AccountId("account-123")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_account_by_id_and_account_exists_then_sync_service_called()
    {
        const string accountIdStr = "account-456";
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetByIdAsync(new AccountId(accountIdStr), Arg.Any<CancellationToken>()).Returns(Option.Some(new AccountEntity
        {
            Id = new AccountId(accountIdStr),
            Profile = AccountProfileFactory.Create("Test User", "test@outlook.com"),
            SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/some/path")),
        }));
        var scheduler = CreateScheduler(mockSyncService, mockRepository, BuildSyncRuleRepository());

        await scheduler.TriggerAccountAsync(accountIdStr, TestContext.Current.CancellationToken);

        await mockSyncService.Received(1).SyncAccountAsync(
            Arg.Is<OneDriveAccount>(a => a.Id == new AccountId(accountIdStr)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_account_by_id_and_account_not_found_then_sync_service_not_called()
    {
        const string accountIdStr = "missing-account";
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetByIdAsync(new AccountId(accountIdStr), Arg.Any<CancellationToken>()).Returns(Option.None<AccountEntity>());
        var scheduler = CreateScheduler(mockSyncService, mockRepository, Substitute.For<ISyncRuleRepository>());

        await scheduler.TriggerAccountAsync(accountIdStr, TestContext.Current.CancellationToken);

        await mockSyncService.DidNotReceive().SyncAccountAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_account_by_id_and_account_exists_then_sync_started_event_raised()
    {
        const string accountIdStr = "account-789";
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetByIdAsync(new AccountId(accountIdStr), Arg.Any<CancellationToken>()).Returns(Option.Some(new AccountEntity { Id = new AccountId(accountIdStr), Profile = AccountProfileFactory.Create("Test", "test@test.com") }));
        var scheduler = CreateScheduler(mockSyncService, mockRepository, BuildSyncRuleRepository());
        string? raisedId = null;
        scheduler.SyncStarted += (_, id) => raisedId = id;

        await scheduler.TriggerAccountAsync(accountIdStr, TestContext.Current.CancellationToken);

        raisedId.ShouldBe(accountIdStr);
    }

    [Fact]
    public async Task when_trigger_account_by_id_and_account_exists_then_sync_completed_event_raised()
    {
        const string accountIdStr = "account-789";
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetByIdAsync(new AccountId(accountIdStr), Arg.Any<CancellationToken>()).Returns(Option.Some(new AccountEntity { Id = new AccountId(accountIdStr), Profile = AccountProfileFactory.Create("Test", "test@test.com") }));
        var scheduler = CreateScheduler(mockSyncService, mockRepository, BuildSyncRuleRepository());
        string? raisedId = null;
        scheduler.SyncCompleted += (_, id) => raisedId = id;

        await scheduler.TriggerAccountAsync(accountIdStr, TestContext.Current.CancellationToken);

        raisedId.ShouldBe(accountIdStr);
    }

    [Fact]
    public async Task when_trigger_account_by_id_then_all_entity_fields_mapped_to_account()
    {
        const string accountIdStr = "account-map-test";
        var lastSyncedAt = new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetByIdAsync(new AccountId(accountIdStr), Arg.Any<CancellationToken>()).Returns(Option.Some(new AccountEntity
        {
            Id = new AccountId(accountIdStr),
            Profile = AccountProfileFactory.Create("Map Test User", "maptest@outlook.com"),
            AccentIndex = 3,
            IsActive = true,
            LastSyncedAt = lastSyncedAt,
            SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/sync/path")),
        }));
        var scheduler = CreateScheduler(mockSyncService, mockRepository, BuildSyncRuleRepository());

        await scheduler.TriggerAccountAsync(accountIdStr, TestContext.Current.CancellationToken);

        await mockSyncService.Received(1).SyncAccountAsync(
            Arg.Is<OneDriveAccount>(a =>
                a.Id == new AccountId(accountIdStr) &&
                a.Profile.DisplayName == "Map Test User" &&
                a.Profile.Email == "maptest@outlook.com" &&
                a.AccentIndex == 3 &&
                a.IsActive == true &&
                a.LastSyncedAt == (Option<DateTimeOffset>)lastSyncedAt &&
                a.SyncConfig.Match(c => c.ConflictPolicy == ConflictPolicy.Ignore, () => false)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_account_by_id_and_local_sync_path_empty_then_mapped_account_has_null_sync_config()
    {
        const string accountIdStr = "account-no-path";
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetByIdAsync(new AccountId(accountIdStr), Arg.Any<CancellationToken>()).Returns(Option.Some(new AccountEntity
        {
            Id = new AccountId(accountIdStr),
            Profile = AccountProfileFactory.Create("No Path User", "nopath@outlook.com"),
            SyncConfig = AccountSyncConfigFactory.Default,
        }));
        var scheduler = CreateScheduler(mockSyncService, mockRepository, BuildSyncRuleRepository());

        await scheduler.TriggerAccountAsync(accountIdStr, TestContext.Current.CancellationToken);

        await mockSyncService.Received(1).SyncAccountAsync(
            Arg.Is<OneDriveAccount>(a => a.SyncConfig.Match(c => false, () => true)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_account_by_id_and_rules_have_include_entries_then_selected_folder_ids_populated()
    {
        const string accountIdStr = "account-with-rules";
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetByIdAsync(new AccountId(accountIdStr), Arg.Any<CancellationToken>()).Returns(Option.Some(new AccountEntity
        {
            Id = new AccountId(accountIdStr),
            Profile = AccountProfileFactory.Create("Rules User", "rules@outlook.com"),
        }));
        var rulesRepo = Substitute.For<ISyncRuleRepository>();
        rulesRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                new() { RuleType = RuleType.Include, RemoteItemId = "folder-1" },
                new() { RuleType = RuleType.Include, RemoteItemId = "folder-2" },
                new() { RuleType = RuleType.Exclude, RemoteItemId = "folder-3" },
                new() { RuleType = RuleType.Include, RemoteItemId = null },
            ]);
        var scheduler = CreateScheduler(mockSyncService, mockRepository, rulesRepo);

        await scheduler.TriggerAccountAsync(accountIdStr, TestContext.Current.CancellationToken);

        await mockSyncService.Received(1).SyncAccountAsync(
            Arg.Is<OneDriveAccount>(a => a.SelectedFolderIds.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_now_and_entity_has_accent_index_then_mapped_account_has_correct_accent_index()
    {
        const string accountIdStr = "account-accent";
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([new AccountEntity
        {
            Id          = new AccountId(accountIdStr),
            Profile     = AccountProfileFactory.Create("Accent User", "accent@outlook.com"),
            AccentIndex = 5,
            IsActive    = true,
        }]);
        var scheduler = CreateScheduler(mockSyncService, mockRepository, BuildSyncRuleRepository());

        await scheduler.TriggerNowAsync(TestContext.Current.CancellationToken);

        await mockSyncService.Received(1).SyncAccountAsync(
            Arg.Is<OneDriveAccount>(a => a.AccentIndex == 5 && a.IsActive == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_cancel_account_called_for_unknown_account_then_no_exception_thrown()
    {
        var scheduler = CreateScheduler(Substitute.For<ISyncService>(), Substitute.For<IAccountRepository>(), Substitute.For<ISyncRuleRepository>());

        await Should.NotThrowAsync(() => scheduler.CancelAccountSyncAsync("non-existent-account"));
    }

    [Fact]
    public async Task when_cancel_account_called_for_active_sync_then_token_passed_to_sync_service_is_cancelled()
    {
        CancellationToken capturedToken = default;
        var syncStarted = new TaskCompletionSource();
        var mockSyncService = Substitute.For<ISyncService>();
        mockSyncService.SyncAccountAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                capturedToken = callInfo.ArgAt<CancellationToken>(1);
                syncStarted.SetResult();
                await Task.Delay(Timeout.Infinite, capturedToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            });

        var scheduler = CreateScheduler(mockSyncService, Substitute.For<IAccountRepository>(), Substitute.For<ISyncRuleRepository>());
        var account = new OneDriveAccount { Id = new AccountId("cancel-test") };

        var triggerTask = scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);
        await syncStarted.Task;
        await scheduler.CancelAccountSyncAsync("cancel-test");
        await triggerTask;

        capturedToken.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task when_trigger_account_called_while_same_account_already_syncing_then_sync_service_not_called_again()
    {
        var syncStarted = new TaskCompletionSource();
        var syncRelease = new TaskCompletionSource();
        var mockSyncService = Substitute.For<ISyncService>();
        mockSyncService.SyncAccountAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                syncStarted.TrySetResult();
                await syncRelease.Task;
            });

        var scheduler = CreateScheduler(mockSyncService, Substitute.For<IAccountRepository>(), Substitute.For<ISyncRuleRepository>());
        var account = new OneDriveAccount { Id = new AccountId("dup-account") };

        var firstSync = scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);
        await syncStarted.Task;

        var secondSync = scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        syncRelease.SetResult();
        await firstSync;
        await secondSync;

        await mockSyncService.Received(1).SyncAccountAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_scheduled_sync_runs_while_account_already_syncing_manually_then_account_is_skipped()
    {
        const string accountId = "shared-account";
        var syncStarted = new TaskCompletionSource();
        var syncRelease = new TaskCompletionSource();
        var callCount = 0;
        var mockSyncService = Substitute.For<ISyncService>();
        mockSyncService.SyncAccountAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                Interlocked.Increment(ref callCount);
                syncStarted.TrySetResult();
                await syncRelease.Task;
            });

        var mockRepository = Substitute.For<IAccountRepository>();
        mockRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([new AccountEntity { Id = new AccountId(accountId), Profile = AccountProfileFactory.Create("Test", "test@test.com") }]);

        var scheduler = CreateScheduler(mockSyncService, mockRepository, BuildSyncRuleRepository());
        var account = new OneDriveAccount { Id = new AccountId(accountId) };

        var manualSync = scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);
        await syncStarted.Task;

        var scheduledPass = scheduler.TriggerNowAsync(TestContext.Current.CancellationToken);

        syncRelease.SetResult();
        await manualSync;
        await scheduledPass;

        callCount.ShouldBe(1);
    }

    private static ISyncRuleRepository BuildSyncRuleRepository()
    {
        var repo = Substitute.For<ISyncRuleRepository>();
        repo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        return repo;
    }
}
