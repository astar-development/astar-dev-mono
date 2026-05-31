using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Dashboard;

public sealed class GivenADashboardViewModel
{
    private readonly ISyncScheduler _scheduler = Substitute.For<ISyncScheduler>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();

    private DashboardViewModel CreateSut() => new(_scheduler, _localizationService, _accountRepository, _syncEventAggregator);

    private static OneDriveAccount NeverSyncedAccount(string id) => new() { Id = new AccountId(id) };

    private static OneDriveAccount RecentlySyncedAccount(string id) => new() { Id = new AccountId(id), LastSyncedAt = DateTimeOffset.UtcNow.AddSeconds(-30) };

    [Fact]
    public void when_constructed_then_localization_service_receives_common_never_key_for_last_sync_text()
    {
        _ = CreateSut();

        _localizationService.Received(1).GetLocal("Common.Never");
    }

    [Fact]
    public void when_overall_status_text_accessed_while_syncing_then_status_bar_syncing_key_is_used()
    {
        var sut = CreateSut();
        sut.AnySyncing = true;

        _ = sut.OverallStatusText;

        _localizationService.Received(1).GetLocal("StatusBar.Syncing");
    }

    [Fact]
    public void when_overall_status_text_accessed_with_errors_then_status_bar_error_key_is_used()
    {
        var sut = CreateSut();
        sut.AnyErrors = true;

        _ = sut.OverallStatusText;

        _localizationService.Received(1).GetLocal("StatusBar.Error");
    }

    [Fact]
    public void when_overall_status_text_accessed_with_single_conflict_then_status_bar_conflict_key_is_used()
    {
        var sut = CreateSut();
        sut.TotalConflicts = 1;

        _ = sut.OverallStatusText;

        _localizationService.Received(1).GetLocal("StatusBar.Conflict", Arg.Any<object[]>());
    }

    [Fact]
    public void when_overall_status_text_accessed_with_multiple_conflicts_then_status_bar_conflicts_key_is_used()
    {
        var sut = CreateSut();
        sut.TotalConflicts = 3;

        _ = sut.OverallStatusText;

        _localizationService.Received(1).GetLocal("StatusBar.Conflicts", Arg.Any<object[]>());
    }

    [Fact]
    public void when_overall_status_text_accessed_with_no_issues_then_dashboard_all_synced_key_is_used()
    {
        var sut = CreateSut();

        _ = sut.OverallStatusText;

        _localizationService.Received(1).GetLocal("Dashboard.AllSynced");
    }

    [Fact]
    public void when_add_never_synced_account_then_recalculate_globals_uses_common_never_key_for_last_sync_text()
    {
        _localizationService.GetLocal("Common.NeverSynced").Returns("Never synced");
        _localizationService.GetLocal("Common.Never").Returns("Never");
        var sut = CreateSut();

        sut.AddAccount(NeverSyncedAccount("acc-1"));

        _localizationService.Received().GetLocal("Common.Never");
    }

    [Fact]
    public void when_add_synced_account_then_last_sync_text_equals_synced_accounts_text()
    {
        _localizationService.GetLocal("Common.JustNow").Returns("Just now");
        var sut = CreateSut();

        sut.AddAccount(RecentlySyncedAccount("acc-1"));

        sut.LastSyncText.ShouldBe("Just now");
    }

    [Fact]
    public void when_one_account_synced_and_one_never_synced_then_last_sync_text_is_synced_accounts_text()
    {
        _localizationService.GetLocal("Common.NeverSynced").Returns("Never synced");
        _localizationService.GetLocal("Common.JustNow").Returns("Just now");
        var sut = CreateSut();

        sut.AddAccount(RecentlySyncedAccount("acc-synced"));
        sut.AddAccount(NeverSyncedAccount("acc-never"));

        sut.LastSyncText.ShouldBe("Just now");
    }

    [Fact]
    public void when_all_accounts_have_never_synced_then_last_sync_text_comes_from_common_never_key()
    {
        _localizationService.GetLocal("Common.NeverSynced").Returns("Never synced");
        _localizationService.GetLocal("Common.Never").Returns("Never");
        var sut = CreateSut();

        sut.AddAccount(NeverSyncedAccount("acc-1"));
        sut.AddAccount(NeverSyncedAccount("acc-2"));

        _localizationService.Received().GetLocal("Common.Never");
    }
}
