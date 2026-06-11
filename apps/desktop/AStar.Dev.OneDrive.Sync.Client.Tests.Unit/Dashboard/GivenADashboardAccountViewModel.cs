using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Dashboard;

public sealed class GivenADashboardAccountViewModel
{
    private static DashboardAccountViewModel CreateSut(ISyncScheduler scheduler, ILocalizationService? localization = null)
        => new(
            new OneDriveAccount { Id = new AccountId("test-account") },
            scheduler,
            Substitute.For<IAccountRepository>(),
            localization ?? Substitute.For<ILocalizationService>(),
            Substitute.For<IActivityItemViewModelFactory>());

    private static DashboardAccountViewModel CreateSutWithAccount(OneDriveAccount account, ILocalizationService localization)
        => new(account, Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), localization, new ActivityItemViewModelFactory(localization));

    [Fact]
    public async Task when_cancel_sync_command_invoked_then_scheduler_cancel_account_called_with_correct_id()
    {
        var mockScheduler = Substitute.For<ISyncScheduler>();
        var sut = CreateSut(mockScheduler);

        await ((IAsyncRelayCommand)sut.CancelSyncCommand).ExecuteAsync(null);

        await mockScheduler.Received(1).CancelAccountSyncAsync("test-account");
    }

    [Fact]
    public void when_status_label_accessed_with_syncing_state_then_localization_receives_status_bar_syncing_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = CreateSut(Substitute.For<ISyncScheduler>(), localization);

        sut.SyncState = SyncState.Syncing;
        _ = sut.StatusLabel;

        localization.Received(1).GetLocal("StatusBar.Syncing");
    }

    [Fact]
    public void when_status_label_accessed_with_error_state_then_localization_receives_status_bar_error_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = CreateSut(Substitute.For<ISyncScheduler>(), localization);

        sut.SyncState = SyncState.Error;
        _ = sut.StatusLabel;

        localization.Received(1).GetLocal("StatusBar.Error");
    }

    [Fact]
    public void when_status_label_accessed_with_one_conflict_then_localization_receives_status_bar_conflict_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = CreateSut(Substitute.For<ISyncScheduler>(), localization);

        sut.SyncState = SyncState.Conflict;
        sut.ConflictCount = 1;
        _ = sut.StatusLabel;

        localization.Received(1).GetLocal("StatusBar.Conflict", Arg.Any<object[]>());
    }

    [Fact]
    public void when_status_label_accessed_with_multiple_conflicts_then_localization_receives_status_bar_conflicts_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = CreateSut(Substitute.For<ISyncScheduler>(), localization);

        sut.SyncState = SyncState.Conflict;
        sut.ConflictCount = 3;
        _ = sut.StatusLabel;

        localization.Received(1).GetLocal("StatusBar.Conflicts", Arg.Any<object[]>());
    }

    [Fact]
    public void when_status_label_accessed_with_pending_state_then_localization_receives_dashboard_pending_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = CreateSut(Substitute.For<ISyncScheduler>(), localization);

        sut.SyncState = SyncState.Pending;
        _ = sut.StatusLabel;

        localization.Received(1).GetLocal("Dashboard.Pending");
    }

    [Fact]
    public void when_status_label_accessed_with_idle_and_no_conflicts_then_localization_receives_status_bar_synced_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = CreateSut(Substitute.For<ISyncScheduler>(), localization);

        sut.SyncState = SyncState.Idle;
        _ = sut.StatusLabel;

        localization.Received(1).GetLocal("StatusBar.Synced");
    }

    [Fact]
    public void when_storage_quota_total_is_zero_then_localization_receives_common_unknown_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = CreateSut(Substitute.For<ISyncScheduler>(), localization);

        _ = sut.StorageText;

        localization.Received(1).GetLocal("Common.Unknown");
    }

    [Fact]
    public void when_constructed_with_no_sync_history_then_localization_receives_common_never_synced_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var account = new OneDriveAccount { Id = new AccountId("test-account"), LastSyncedAt = null };

        _ = CreateSutWithAccount(account, localization);

        localization.Received(1).GetLocal("Common.NeverSynced");
    }

    [Fact]
    public void when_update_sync_state_called_with_no_sync_path_configured_then_localization_receives_dashboard_no_sync_path_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = CreateSut(Substitute.For<ISyncScheduler>(), localization);

        sut.UpdateSyncState(SyncState.NoSyncPathConfigured, 0);

        localization.Received(1).GetLocal("Dashboard.NoSyncPath");
    }

    [Fact]
    public void when_constructed_with_sync_within_sixty_seconds_then_localization_receives_common_just_now_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var account = new OneDriveAccount { Id = new AccountId("test-account"), LastSyncedAt = DateTimeOffset.UtcNow.AddSeconds(-30) };

        _ = CreateSutWithAccount(account, localization);

        localization.Received(1).GetLocal("Common.JustNow");
    }

    [Fact]
    public void when_constructed_with_sync_thirty_minutes_ago_then_localization_receives_common_minutes_ago_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var account = new OneDriveAccount { Id = new AccountId("test-account"), LastSyncedAt = DateTimeOffset.UtcNow.AddMinutes(-30) };

        _ = CreateSutWithAccount(account, localization);

        localization.Received(1).GetLocal("Common.MinutesAgo", Arg.Any<object[]>());
    }

    [Fact]
    public void when_constructed_with_sync_three_hours_ago_then_localization_receives_common_hours_ago_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var account = new OneDriveAccount { Id = new AccountId("test-account"), LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-3) };

        _ = CreateSutWithAccount(account, localization);

        localization.Received(1).GetLocal("Common.HoursAgo", Arg.Any<object[]>());
    }

    [Fact]
    public void when_constructed_with_sync_one_day_ago_then_localization_receives_common_yesterday_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var account = new OneDriveAccount { Id = new AccountId("test-account"), LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-24) };

        _ = CreateSutWithAccount(account, localization);

        localization.Received(1).GetLocal("Common.Yesterday");
    }

    [Fact]
    public void when_constructed_with_sync_five_days_ago_then_localization_receives_common_days_ago_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        var account = new OneDriveAccount { Id = new AccountId("test-account"), LastSyncedAt = DateTimeOffset.UtcNow.AddDays(-5) };

        _ = CreateSutWithAccount(account, localization);

        localization.Received(1).GetLocal("Common.DaysAgo", Arg.Any<object[]>());
    }
}
