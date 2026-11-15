using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.CloudSync.ViewModels.TestsUnit.Overview;
using AStarDev.CloudSync.ViewModels.Tray;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Tray;

public sealed class GivenATrayIconState
{
    private static readonly CloudAccount Photos = AttentionItems.Account("Photos");

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static CloudAccount WithStatus(SyncStatus status) => TestData.CreateAccount(status, Option.None<DateTimeOffset>());

    private static async Task<TrayIconState> IconFor(IReadOnlyList<CloudAccount> accounts, IReadOnlyList<SyncAttentionItem> conflicts)
    {
        var overview = SyncOverviewFactory.Create(accounts, [], [], conflicts, [], 0, Option.None<DateTimeOffset>());
        var viewModel = new SyncOverviewViewModel(new FixedSyncOverviewSource(overview), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await viewModel.LoadAsync(CancellationToken);
        var host = new RecordingTrayHost();

        return new TrayMenuModel(viewModel, new RecordingSyncActions(), host, host, TestData.CreateLocalization()).IconState;
    }

    [Fact]
    public async Task when_nothing_syncs_and_nothing_needs_the_user_then_the_icon_is_idle()
    {
        var icon = await IconFor([Photos], []);

        icon.ShouldBe(TrayIconStateFactory.CreateIdle());
    }

    [Fact]
    public async Task when_there_are_no_accounts_then_the_icon_is_idle()
    {
        var icon = await IconFor([], []);

        icon.ShouldBe(TrayIconStateFactory.CreateIdle());
    }

    [Fact]
    public async Task when_an_account_is_syncing_then_the_icon_is_syncing()
    {
        var icon = await IconFor([Photos, WithStatus(SyncStatusFactory.CreateSyncing(40))], []);

        icon.ShouldBe(TrayIconStateFactory.CreateSyncing());
    }

    [Fact]
    public async Task when_items_need_attention_then_the_icon_is_attention()
    {
        var icon = await IconFor([Photos], [AttentionItems.CreateConflict(Photos.Id, "Docs/a.txt")]);

        icon.ShouldBe(TrayIconStateFactory.CreateAttention());
    }

    [Fact]
    public async Task when_an_account_needs_sign_in_then_the_icon_is_attention()
    {
        var icon = await IconFor([TestData.CreateAccountNeedingSignIn()], []);

        icon.ShouldBe(TrayIconStateFactory.CreateAttention());
    }

    [Fact]
    public async Task when_an_account_failed_then_the_icon_is_faulted()
    {
        var icon = await IconFor([Photos, WithStatus(SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateUnexpected()))], []);

        icon.ShouldBe(TrayIconStateFactory.CreateFaulted());
    }

    [Fact]
    public async Task when_an_account_failed_and_another_needs_sign_in_then_the_failure_wins()
    {
        var icon = await IconFor([TestData.CreateAccountNeedingSignIn(), WithStatus(SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateUnexpected()))], [AttentionItems.CreateConflict(Photos.Id, "Docs/a.txt")]);

        icon.ShouldBe(TrayIconStateFactory.CreateFaulted());
    }

    [Fact]
    public async Task when_one_account_syncs_and_another_needs_sign_in_then_sign_in_wins_like_the_tooltip()
    {
        var icon = await IconFor([WithStatus(SyncStatusFactory.CreateSyncing(40)), TestData.CreateAccountNeedingSignIn()], []);

        icon.ShouldBe(TrayIconStateFactory.CreateAttention());
    }

    [Fact]
    public async Task when_accounts_sync_and_conflicts_wait_then_the_icon_is_syncing_like_the_tooltip()
    {
        var icon = await IconFor([WithStatus(SyncStatusFactory.CreateSyncing(40))], [AttentionItems.CreateConflict(Photos.Id, "Docs/a.txt")]);

        icon.ShouldBe(TrayIconStateFactory.CreateSyncing());
    }

    [Fact]
    public async Task when_the_overview_reloads_then_the_icon_change_is_raised()
    {
        var source = new SwitchableSource(SyncOverviewFactory.Create([Photos], [], [], [], [], 0, Option.None<DateTimeOffset>()));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(CancellationToken);
        var host = new RecordingTrayHost();
        var model = new TrayMenuModel(overview, new RecordingSyncActions(), host, host, TestData.CreateLocalization());
        var changed = new List<string?>();
        model.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        source.Overview = SyncOverviewFactory.Create([WithStatus(SyncStatusFactory.CreateSyncing(10))], [], [], [], [], 0, Option.None<DateTimeOffset>());

        await overview.LoadAsync(CancellationToken);

        (model.IconState, changed.Contains(nameof(TrayMenuModel.IconState))).ShouldBe((TrayIconStateFactory.CreateSyncing(), true));
    }

    private sealed class SwitchableSource(SyncOverview initial) : ISyncOverviewSource
    {
        public SyncOverview Overview { get; set; } = initial;

        public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(Overview));
    }
}
