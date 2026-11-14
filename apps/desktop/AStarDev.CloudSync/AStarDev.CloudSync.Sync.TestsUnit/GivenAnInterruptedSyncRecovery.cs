using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnInterruptedSyncRecovery : IDisposable
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private readonly InMemorySyncRunJournal journal = new();
    private readonly List<SyncRunServices> restarts = [];
    private readonly SyncRunServices services;

    public GivenAnInterruptedSyncRecovery() => services = new SyncRunServices(journal);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private AccountId Personal => services.Personal;

    private AccountId Work => services.Work;

    public void Dispose()
    {
        services.Dispose();

        foreach (var restart in restarts)
            restart.Dispose();
    }

    private SyncRunServices Restart()
    {
        journal.Restart();
        var next = new SyncRunServices(journal, services.Baseline);
        restarts.Add(next);

        return next;
    }

    private async Task LeaveUnfinishedAsync(AccountId accountId) => _ = await journal.StartAsync(accountId, SyncTriggerFactory.CreateScheduled(), CancellationToken);

    private static string Describe(RecoveryOutcome outcome) => outcome switch
    {
        RecoveryOutcome.Resumed resumed => $"resumed {resumed.AccountId.Value}",
        RecoveryOutcome.NotResumed notResumed => $"not resumed {notResumed.AccountId.Value} {notResumed.Reason.GetType().Name}",
        _ => "unknown",
    };

    [Theory]
    [InlineData(1, "index")]
    [InlineData(2, "scan")]
    [InlineData(3, "download")]
    [InlineData(4, "upload")]
    public async Task when_a_pass_is_killed_at_a_step_then_the_next_start_finds_it_interrupted_and_runs_a_recovery_pass(int killedAtStepNumber, string step)
    {
        var reached = 0;
        services.Steps.OnStep = () =>
        {
            if (++reached == killedAtStepNumber)
                journal.Kill();
        };
        _ = await services.Runner.RunAsync(Personal, SyncTriggerFactory.CreateScheduled(), CancellationToken);
        var next = Restart();

        var interrupted = (await journal.GetInterruptedAsync(CancellationToken)).Match(found => found, _ => []);
        var outcomes = await next.Recovery.RecoverAsync(CancellationToken);

        interrupted.Select(run => run.AccountId).ShouldBe([Personal], step);
        outcomes.Select(Describe).ShouldBe([$"resumed {Personal.Value}"], step);
        journal.Runs.Select(run => (run.Trigger.GetType().Name, run.Outcome)).ShouldBe([(nameof(SyncTrigger.Scheduled), Option.Some(SyncRunOutcomeFactory.CreateInterrupted())), (nameof(SyncTrigger.Recovery), Option.Some(SyncRunOutcomeFactory.CreateCompleted()))], step);
    }

    [Fact]
    public async Task when_a_run_is_interrupted_then_the_user_is_told_in_the_activity_feed()
    {
        await LeaveUnfinishedAsync(Personal);
        var next = Restart();

        _ = await next.Recovery.RecoverAsync(CancellationToken);

        next.ActivityLog.Appended.Select(entry => (entry.AccountId, entry.Kind)).ShouldBe([(Personal, ActivityKindFactory.CreateSyncInterrupted())]);
    }

    [Fact]
    public async Task when_nothing_was_interrupted_then_nothing_runs_and_the_feed_is_untouched()
    {
        _ = await services.Runner.RunAsync(Personal, SyncTriggerFactory.CreateManual(), CancellationToken);
        var next = Restart();

        var outcomes = await next.Recovery.RecoverAsync(CancellationToken);

        outcomes.ShouldBeEmpty();
        (journal.Runs.Count, next.ActivityLog.Calls).ShouldBe((1, 0));
    }

    [Fact]
    public async Task when_recovery_has_run_then_the_interrupted_run_is_not_found_again()
    {
        await LeaveUnfinishedAsync(Personal);
        var next = Restart();
        _ = await next.Recovery.RecoverAsync(CancellationToken);

        var again = await next.Recovery.RecoverAsync(CancellationToken);

        again.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_two_accounts_were_interrupted_then_their_recovery_passes_are_spread_across_the_stagger_window()
    {
        await LeaveUnfinishedAsync(Personal);
        await LeaveUnfinishedAsync(Work);
        var next = Restart();

        var recovering = Task.Run(() => next.Recovery.RecoverAsync(CancellationToken), CancellationToken);
        await UntilRecoveryRunsAsync(1).WaitAsync(Limit, CancellationToken);
        var startedBeforeTheWindowPassed = RecoveryRuns();
        next.Time.Advance(RecoveryStagger.Window);
        var outcomes = await recovering.WaitAsync(Limit, CancellationToken);

        (startedBeforeTheWindowPassed, RecoveryRuns(), outcomes.Count).ShouldBe((1, 2, 2));
    }

    [Fact]
    public async Task when_the_journal_cannot_be_read_then_nothing_is_recovered_and_no_pass_runs()
    {
        journal.Fails = true;
        var next = Restart();

        var outcomes = await next.Recovery.RecoverAsync(CancellationToken);

        outcomes.ShouldBeEmpty();
        next.Steps.Steps.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_recovery_pass_cannot_start_then_the_outcome_says_why()
    {
        await LeaveUnfinishedAsync(Personal);
        var next = Restart();
        var gate = new WriteGate();
        next.Steps.DownloadGate = Option.Some(gate);
        var running = next.Runner.RunAsync(Personal, SyncTriggerFactory.CreateManual(), CancellationToken);
        await gate.Entered.WaitAsync(Limit, CancellationToken);

        var outcomes = await next.Recovery.RecoverAsync(CancellationToken);
        gate.Release();
        _ = await running;

        outcomes.Select(Describe).ShouldBe([$"not resumed {Personal.Value} {nameof(SyncPassFailure.AlreadyRunning)}"]);
    }

    private int RecoveryRuns() => journal.Runs.Count(run => run.Trigger is SyncTrigger.Recovery);

    private async Task UntilRecoveryRunsAsync(int count)
    {
        while (RecoveryRuns() < count)
            await Task.Delay(5, CancellationToken);
    }
}
