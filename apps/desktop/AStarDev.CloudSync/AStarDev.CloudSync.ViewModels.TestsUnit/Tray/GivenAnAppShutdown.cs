using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.ViewModels.Tray;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Tray;

public class GivenAnAppShutdown
{
    private readonly InMemoryAccountStore accounts = new();
    private readonly FakeSyncActivitySource activity = new();
    private readonly RecordingSyncActions actions = new();
    private readonly RecordingTrayHost exit = new();
    private RecordingConfirmQuitDialog confirm = new(QuitChoiceFactory.CreateQuitAnyway());

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private AppShutdown Create() => new(accounts, activity, actions, exit, confirm, exit, NullLogger<AppShutdown>.Instance);

    private async Task<AccountId> AddAsync(string name)
    {
        var key = ProviderAccountKeyFactory.Create($"{name}-key").Match(created => created, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(key, CloudProviderFactory.CreateOneDrive(), name, $"{name.ToLowerInvariant()}@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1, 2));

        return (await accounts.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }

    [Fact]
    public async Task when_quit_is_requested_and_nothing_is_syncing_then_the_app_exits_at_once()
    {
        _ = await AddAsync("Photos");
        var shutdown = Create();

        await shutdown.RequestQuitAsync(CancellationToken);

        (exit.Exits, actions.Calls.Count, shutdown.State).ShouldBe((1, 0, ShutdownStateFactory.CreateQuitting()));
    }

    [Fact]
    public async Task when_quit_is_requested_while_a_pass_is_running_then_only_that_pass_is_cancelled_before_the_app_exits()
    {
        var running = await AddAsync("Photos");
        _ = await AddAsync("Work");
        activity.Running.Add(running);

        await Create().RequestQuitAsync(CancellationToken);

        actions.Calls.ShouldBe([$"CancelSyncAsync:{running}"]);
        exit.Exits.ShouldBe(1);
    }

    [Fact]
    public async Task when_nothing_is_syncing_then_the_user_is_not_asked_and_the_window_is_not_shown()
    {
        _ = await AddAsync("Photos");

        await Create().RequestQuitAsync(CancellationToken);

        (confirm.Asked, exit.Shown).ShouldBe((0, 0));
    }

    [Fact]
    public async Task when_a_pass_is_running_then_the_window_is_shown_and_the_user_is_asked_before_anything_is_cancelled()
    {
        var running = await AddAsync("Photos");
        activity.Running.Add(running);

        await Create().RequestQuitAsync(CancellationToken);

        (confirm.Asked, exit.Shown, exit.Exits).ShouldBe((1, 1, 1));
        actions.Calls.ShouldBe([$"CancelSyncAsync:{running}"]);
    }

    [Fact]
    public async Task when_the_user_keeps_syncing_then_nothing_is_cancelled_and_the_app_stays_open()
    {
        var running = await AddAsync("Photos");
        activity.Running.Add(running);
        confirm = new RecordingConfirmQuitDialog(QuitChoiceFactory.CreateStayOpen());
        var shutdown = Create();

        await shutdown.RequestQuitAsync(CancellationToken);

        (actions.Calls.Count, exit.Exits, shutdown.State).ShouldBe((0, 0, ShutdownStateFactory.CreateRunning()));
    }

    [Fact]
    public async Task when_the_user_kept_syncing_then_a_later_quit_asks_again_and_can_exit()
    {
        var running = await AddAsync("Photos");
        activity.Running.Add(running);
        confirm = new RecordingConfirmQuitDialog(QuitChoiceFactory.CreateStayOpen());
        var shutdown = Create();
        await shutdown.RequestQuitAsync(CancellationToken);
        confirm = new RecordingConfirmQuitDialog(QuitChoiceFactory.CreateQuitAnyway());
        var second = Create();

        await second.RequestQuitAsync(CancellationToken);

        (confirm.Asked, exit.Exits).ShouldBe((1, 1));
    }

    [Fact]
    public async Task when_quit_is_requested_twice_then_the_app_exits_once()
    {
        var shutdown = Create();

        await shutdown.RequestQuitAsync(CancellationToken);
        await shutdown.RequestQuitAsync(CancellationToken);

        exit.Exits.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_accounts_cannot_be_read_then_the_app_still_exits()
    {
        var failing = new FakeAccountStore { All = () => Result.Failure<IReadOnlyList<StoredAccount>, ErrorResponse>(new ErrorResponse("down")) };
        var shutdown = new AppShutdown(failing, activity, actions, exit, confirm, exit, NullLogger<AppShutdown>.Instance);

        await shutdown.RequestQuitAsync(CancellationToken);

        (exit.Exits, actions.Calls.Count).ShouldBe((1, 0));
    }

    [Fact]
    public void when_nothing_was_requested_then_the_app_is_running()
    {
        Create().State.ShouldBe(ShutdownStateFactory.CreateRunning());
    }
}
