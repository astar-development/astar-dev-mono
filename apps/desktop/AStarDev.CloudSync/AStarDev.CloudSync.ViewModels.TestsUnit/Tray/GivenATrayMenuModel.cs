using AStarDev.CloudSync.Overview;
using AStarDev.FunctionalParadigm;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.CloudSync.ViewModels.TestsUnit.Overview;
using AStarDev.CloudSync.ViewModels.Tray;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Tray;

public class GivenATrayMenuModel
{
    private static readonly CloudAccount Photos = AttentionItems.Account("Photos");
    private static readonly CloudAccount Work = AttentionItems.Account("Work");

    private readonly RecordingTrayHost host = new();
    private readonly RecordingSyncActions actions = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<SyncOverviewViewModel> LoadedAsync(params CloudAccount[] accounts)
    {
        var overview = new SyncOverviewViewModel(new FixedAccountsSource(accounts), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(CancellationToken);

        return overview;
    }

    private TrayMenuModel Create(SyncOverviewViewModel overview) => new(overview, actions, host, host, TestData.CreateLocalization());

    private static async Task<SyncOverviewViewModel> LoadedAsync(IReadOnlyList<CloudAccount> accounts, IReadOnlyList<SyncAttentionItem> conflicts)
    {
        var overview = SyncOverviewFactory.Create(accounts, [], [], conflicts, [], 0, Option.None<DateTimeOffset>());
        var viewModel = new SyncOverviewViewModel(new FixedSyncOverviewSource(overview), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await viewModel.LoadAsync(CancellationToken);

        return viewModel;
    }

    private static CloudAccount SyncingAccount() => TestData.CreateAccount(SyncStatusFactory.CreateSyncing(40), Option.None<DateTimeOffset>());

    [Theory]
    [InlineData(1, "AStar Dev CloudSync: Syncing 1 account")]
    [InlineData(2, "AStar Dev CloudSync: Syncing 2 accounts")]
    public async Task when_accounts_are_syncing_then_the_tooltip_counts_them(int syncing, string expected)
    {
        var accounts = Enumerable.Range(0, syncing).Select(_ => SyncingAccount()).Append(Photos).ToList();

        var model = Create(await LoadedAsync(accounts, []));

        model.TooltipText.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, "AStar Dev CloudSync: 1 item needs attention")]
    [InlineData(3, "AStar Dev CloudSync: 3 items need attention")]
    public async Task when_nothing_syncs_and_items_need_attention_then_the_tooltip_counts_them(int conflicts, string expected)
    {
        var items = Enumerable.Range(0, conflicts).Select(index => AttentionItems.CreateConflict(Photos.Id, $"Docs/{index}.txt")).ToList();

        var model = Create(await LoadedAsync([Photos], items));

        model.TooltipText.ShouldBe(expected);
    }

    [Fact]
    public async Task when_accounts_sync_and_items_need_attention_then_the_tooltip_says_syncing()
    {
        var model = Create(await LoadedAsync([SyncingAccount()], [AttentionItems.CreateConflict(Photos.Id, "Docs/a.txt")]));

        model.TooltipText.ShouldBe("AStar Dev CloudSync: Syncing 1 account");
    }

    [Fact]
    public async Task when_an_account_needs_sign_in_then_the_tooltip_says_so_even_while_another_syncs()
    {
        var model = Create(await LoadedAsync([SyncingAccount(), TestData.CreateAccountNeedingSignIn()], []));

        model.TooltipText.ShouldBe("AStar Dev CloudSync: Sign-in required");
    }

    [Fact]
    public async Task when_the_overview_reloads_with_a_new_status_then_the_tooltip_changes_and_the_change_is_raised()
    {
        var source = new SwitchableOverviewSource(SyncOverviewFactory.Create([Photos], [], [], [], [], 0, Option.None<DateTimeOffset>()));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(CancellationToken);
        var model = Create(overview);
        var changed = new List<string?>();
        model.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        source.Overview = SyncOverviewFactory.Create([SyncingAccount(), SyncingAccount()], [], [], [], [], 0, Option.None<DateTimeOffset>());

        await overview.LoadAsync(CancellationToken);

        (model.TooltipText, changed.Contains(nameof(TrayMenuModel.TooltipText))).ShouldBe(("AStar Dev CloudSync: Syncing 2 accounts", true));
    }

    private sealed class SwitchableOverviewSource(SyncOverview initial) : ISyncOverviewSource
    {
        public SyncOverview Overview { get; set; } = initial;

        public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(Overview));
    }

    [Fact]
    public async Task when_there_are_no_accounts_then_the_menu_is_open_and_quit()
    {
        var model = Create(await LoadedAsync());

        model.Items.Select(item => item.Text).ShouldBe(["Open AStar Dev CloudSync", "Quit AStar Dev CloudSync"]);
    }

    [Fact]
    public async Task when_there_are_two_accounts_then_each_has_a_sync_now_entry_between_open_and_quit()
    {
        var model = Create(await LoadedAsync(Photos, Work));

        model.Items.Select(item => item.Text).ShouldBe(["Open AStar Dev CloudSync", "Sync now: Photos", "Sync now: Work", "Quit AStar Dev CloudSync"]);
    }

    [Fact]
    public async Task when_a_sync_now_entry_is_chosen_then_only_that_account_syncs()
    {
        var model = Create(await LoadedAsync(Photos, Work));

        await model.Items.Single(item => item.Text == "Sync now: Work").Command.ExecuteAsync(null);

        actions.Calls.ShouldBe([$"SyncNowAsync:{Work.Id}"]);
    }

    [Fact]
    public async Task when_open_is_chosen_then_the_window_is_shown()
    {
        var model = Create(await LoadedAsync());

        await model.Items[0].Command.ExecuteAsync(null);

        (host.Shown, host.QuitRequests).ShouldBe((1, 0));
    }

    [Fact]
    public async Task when_quit_is_chosen_then_a_quit_is_requested()
    {
        var model = Create(await LoadedAsync());

        await model.Items[^1].Command.ExecuteAsync(null);

        (host.QuitRequests, host.Shown).ShouldBe((1, 0));
    }

    [Fact]
    public async Task when_the_overview_reloads_then_the_menu_tooltip_and_icon_are_announced_as_changed()
    {
        var overview = await LoadedAsync(Photos);
        var model = Create(overview);
        var changed = new List<string?>();
        model.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await overview.LoadAsync(CancellationToken);

        changed.ShouldBe([nameof(TrayMenuModel.Items), nameof(TrayMenuModel.TooltipText), nameof(TrayMenuModel.IconState)], ignoreOrder: true);
    }

    [Fact]
    public async Task when_everything_is_up_to_date_then_the_tooltip_says_so()
    {
        var model = Create(await LoadedAsync(Photos));

        model.TooltipText.ShouldBe("AStar Dev CloudSync: Up to date");
    }
}
