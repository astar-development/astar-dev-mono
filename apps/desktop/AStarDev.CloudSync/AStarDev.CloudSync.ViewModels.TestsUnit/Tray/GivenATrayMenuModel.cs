using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Overview;
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
    public async Task when_the_overview_reloads_then_the_menu_and_tooltip_are_announced_as_changed()
    {
        var overview = await LoadedAsync(Photos);
        var model = Create(overview);
        var changed = new List<string?>();
        model.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await overview.LoadAsync(CancellationToken);

        changed.ShouldBe([nameof(TrayMenuModel.Items), nameof(TrayMenuModel.TooltipText)], ignoreOrder: true);
    }

    [Fact]
    public async Task when_everything_is_up_to_date_then_the_tooltip_says_so()
    {
        var model = Create(await LoadedAsync(Photos));

        model.TooltipText.ShouldBe("AStar Dev CloudSync: Up to date");
    }
}
