using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncScheduler : IDisposable
{
    private readonly ScheduleServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static TimeSpan Minutes(int minutes) => TimeSpan.FromMinutes(minutes);

    private Task StartAsync() => services.Scheduler.StartAsync(CancellationToken);

    private IEnumerable<AccountId> RunAccounts => services.Runner.Runs.Select(run => run.AccountId);

    [Fact]
    public async Task when_started_with_one_account_then_the_first_pass_waits_for_the_startup_delay_and_is_a_scheduled_one()
    {
        var account = await services.AddAccountAsync();
        await StartAsync();

        await services.TickAtAsync(Minutes(1));
        await ScheduleServices.NothingElseHappensAsync();
        var runsBefore = services.Runner.Runs.Count;
        await services.TickAtAsync(Minutes(2));
        await services.WaitForRunsAsync(1);

        runsBefore.ShouldBe(0);
        services.Runner.Runs.Single().ShouldSatisfyAllConditions(
            run => run.AccountId.ShouldBe(account),
            run => run.Trigger.ShouldBe(SyncTriggerFactory.CreateScheduled()));
    }

    [Fact]
    public async Task when_four_accounts_share_the_default_hour_then_they_start_fifteen_minutes_apart()
    {
        var accounts = new List<AccountId>();
        for (var index = 0; index < 4; index++)
            accounts.Add(await services.AddAccountAsync());

        await StartAsync();

        foreach (var (account, index) in accounts.Select((account, index) => (account, index)))
        {
            await services.TickAtAsync(Minutes(2 + (15 * index)));
            await services.WaitForRunsAsync(index + 1);
            RunAccounts.ShouldBe(accounts.Take(index + 1));
        }
    }

    [Fact]
    public async Task when_an_account_ran_then_it_runs_again_one_interval_later_and_not_before()
    {
        _ = await services.AddAccountAsync();
        await StartAsync();
        await services.TickAtAsync(Minutes(2));
        await services.WaitForRunsAsync(1);

        await services.TickAtAsync(Minutes(61));
        await ScheduleServices.NothingElseHappensAsync();
        var beforeTheInterval = services.Runner.Runs.Count;
        await services.TickAtAsync(Minutes(62));
        await services.WaitForRunsAsync(2);

        beforeTheInterval.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_interval_is_changed_then_the_new_interval_applies_without_a_restart()
    {
        _ = await services.AddAccountAsync();
        await StartAsync();
        await services.TickAtAsync(Minutes(2));
        await services.WaitForRunsAsync(1);

        _ = await services.Settings.SetAsync(SyncScheduleFactory.CreateEvery(Minutes(30)), CancellationToken);
        await services.TickAtAsync(Minutes(10));
        await ScheduleServices.NothingElseHappensAsync();
        await services.TickAtAsync(Minutes(32));
        await services.WaitForRunsAsync(2);

        services.Runner.Runs.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_schedule_is_manual_only_then_no_pass_ever_starts()
    {
        _ = await services.AddAccountAsync();
        _ = await services.Settings.SetAsync(SyncScheduleFactory.CreateManualOnly(), CancellationToken);
        await StartAsync();

        await services.TickAtAsync(Minutes(300));
        await ScheduleServices.NothingElseHappensAsync();

        services.Runner.Runs.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_schedule_cannot_be_read_then_no_pass_starts_and_a_warning_is_logged_without_the_error_text()
    {
        _ = await services.AddAccountAsync();
        services.Settings.Current = Result.Failure<SyncSchedule, ErrorResponse>(new ErrorResponse("secret-db-path failed"));
        await StartAsync();

        await services.TickAtAsync(Minutes(2));
        await services.WaitForLogAsync("schedule");

        services.Runner.Runs.ShouldBeEmpty();
        services.Logs.Logger.Messages.ShouldNotContain(message => message.Contains("secret-db-path"));
    }

    [Fact]
    public async Task when_an_account_is_added_later_then_the_offsets_are_recalculated_and_it_starts_at_its_new_offset()
    {
        var first = await services.AddAccountAsync();
        await StartAsync();
        await services.TickAtAsync(Minutes(2));
        await services.WaitForRunsAsync(1);
        var second = await services.AddAccountAsync();

        await services.TickAtAsync(Minutes(10));
        await ScheduleServices.NothingElseHappensAsync();
        await services.TickAtAsync(Minutes(32));
        await services.WaitForRunsAsync(2);

        RunAccounts.ShouldBe([first, second]);
    }

    [Fact]
    public async Task when_an_account_is_removed_then_it_is_never_triggered_again_and_the_others_are_not_affected()
    {
        var first = await services.AddAccountAsync();
        var second = await services.AddAccountAsync();
        await StartAsync();
        await services.TickAtAsync(Minutes(2));
        await services.WaitForRunsAsync(1);
        await services.RemoveAccountAsync(second);

        await services.TickAtAsync(Minutes(40));
        await ScheduleServices.NothingElseHappensAsync();
        await services.TickAtAsync(Minutes(62));
        await services.WaitForRunsAsync(2);

        RunAccounts.ShouldBe([first, first]);
    }

    [Fact]
    public async Task when_an_account_is_already_syncing_at_its_tick_then_no_second_pass_starts_and_the_skip_is_logged()
    {
        var account = await services.AddAccountAsync();
        services.Activity.SetRunning(account);
        await StartAsync();

        await services.TickAtAsync(Minutes(2));
        await services.WaitForLogAsync("already syncing");

        services.Runner.Runs.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_an_account_is_paused_then_it_is_skipped_with_the_reason_logged_and_runs_again_once_resumed()
    {
        var account = await services.AddAccountAsync();
        services.Paused.Paused = Result.Success<IReadOnlyList<AccountId>, ErrorResponse>([account]);
        await StartAsync();

        await services.TickAtAsync(Minutes(2));
        await services.WaitForLogAsync("paused");
        services.Paused.Paused = Result.Success<IReadOnlyList<AccountId>, ErrorResponse>([]);
        await services.TickAtAsync(Minutes(62));
        await services.WaitForRunsAsync(1);

        RunAccounts.ShouldBe([account]);
    }

    [Fact]
    public async Task when_an_account_needs_sign_in_then_it_is_skipped_with_the_reason_logged()
    {
        var account = await services.AddAccountAsync();
        services.SignIn.Record(account, AccountSignInStateFactory.CreateSignInRequired());
        await StartAsync();

        await services.TickAtAsync(Minutes(2));
        await services.WaitForLogAsync("sign in");

        services.Runner.Runs.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_an_accounts_sync_folder_is_missing_then_it_is_skipped_with_the_reason_logged()
    {
        var account = await services.AddAccountAsync();
        services.DeleteRootFolder(account);
        await StartAsync();

        await services.TickAtAsync(Minutes(2));
        await services.WaitForLogAsync("folder");

        services.Runner.Runs.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_an_account_has_no_sync_folder_set_then_it_is_skipped_with_the_reason_logged()
    {
        var account = await services.AddAccountAsync();
        services.ForgetRoot(account);
        await StartAsync();

        await services.TickAtAsync(Minutes(2));
        await services.WaitForLogAsync("folder");

        services.Runner.Runs.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_skipped_account_stops_being_skipped_then_the_others_were_not_held_back()
    {
        var paused = await services.AddAccountAsync();
        var active = await services.AddAccountAsync();
        services.Paused.Paused = Result.Success<IReadOnlyList<AccountId>, ErrorResponse>([paused]);
        await StartAsync();

        await services.TickAtAsync(Minutes(32));
        await services.WaitForRunsAsync(1);

        RunAccounts.ShouldBe([active]);
    }

    [Fact]
    public async Task when_the_machine_sleeps_for_three_hours_then_exactly_one_pass_runs_per_account()
    {
        var first = await services.AddAccountAsync();
        var second = await services.AddAccountAsync();
        await StartAsync();

        await services.TickAtAsync(TimeSpan.FromHours(3));
        await services.WaitForRunsAsync(2);
        await ScheduleServices.NothingElseHappensAsync();

        RunAccounts.ShouldBe([first, second], ignoreOrder: true);
    }

    [Fact]
    public async Task when_the_machine_slept_then_each_account_keeps_its_own_offset_for_the_next_pass()
    {
        var first = await services.AddAccountAsync();
        var second = await services.AddAccountAsync();
        await StartAsync();
        await services.TickAtAsync(TimeSpan.FromHours(3));
        await services.WaitForRunsAsync(2);

        await services.TickAtAsync(TimeSpan.FromHours(3) + Minutes(1));
        await ScheduleServices.NothingElseHappensAsync();
        var beforeTheFirstOffset = services.Runner.Runs.Count;
        await services.TickAtAsync(TimeSpan.FromHours(3) + Minutes(2));
        await services.WaitForRunsAsync(3);
        await services.TickAtAsync(TimeSpan.FromHours(3) + Minutes(32));
        await services.WaitForRunsAsync(4);

        beforeTheFirstOffset.ShouldBe(2);
        RunAccounts.Skip(2).ShouldBe([first, second]);
    }

    [Fact]
    public async Task when_the_scheduler_stops_then_a_running_pass_is_cancelled_and_waited_for_and_no_pass_starts_afterwards()
    {
        var account = await services.AddAccountAsync();
        var gate = new WriteGate();
        services.Runner.Hold = Option.Some(gate);
        await StartAsync();
        await services.TickAtAsync(Minutes(2));
        await gate.Entered.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
        var token = services.Runner.Runs.Single().Token;

        await services.Scheduler.StopAsync(CancellationToken);
        await services.TickAtAsync(Minutes(62));
        await ScheduleServices.NothingElseHappensAsync();

        token.IsCancellationRequested.ShouldBeTrue();
        services.Runner.Runs.Count.ShouldBe(1);
        services.Runner.Runs.Single().AccountId.ShouldBe(account);
    }

    [Fact]
    public async Task when_stop_is_called_without_a_start_then_it_does_nothing()
    {
        await services.Scheduler.StopAsync(CancellationToken);

        services.Runner.Runs.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_start_is_called_twice_then_each_account_still_gets_one_pass_per_tick()
    {
        _ = await services.AddAccountAsync();
        await StartAsync();
        await StartAsync();

        await services.TickAtAsync(Minutes(2));
        await services.WaitForRunsAsync(1);
        await ScheduleServices.NothingElseHappensAsync();

        services.Runner.Runs.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_many_ticks_arrive_at_once_then_each_account_still_gets_one_pass()
    {
        _ = await services.AddAccountAsync();
        await StartAsync();

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => services.Time.Advance(TimeSpan.FromSeconds(30)), CancellationToken)));
        await services.WaitForRunsAsync(1);
        await ScheduleServices.NothingElseHappensAsync();

        services.Runner.Runs.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_pass_is_started_then_the_logs_carry_no_account_details()
    {
        var account = await services.AddAccountAsync();
        services.Paused.Paused = Result.Success<IReadOnlyList<AccountId>, ErrorResponse>([account]);
        await StartAsync();

        await services.TickAtAsync(Minutes(2));
        await services.WaitForLogAsync("paused");

        services.Logs.Logger.Messages.ShouldNotContain(message => message.Contains(account.ToString()) || message.Contains(AccountFixtures.Email));
    }

    public void Dispose() => services.Dispose();
}
