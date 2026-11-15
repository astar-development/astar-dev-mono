using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnInterruptedSyncRecoveryThatIsBlocked : IDisposable
{
    private readonly InMemorySyncRunJournal journal = new();
    private readonly List<SyncRunServices> restarts = [];
    private readonly SyncRunServices first;

    public GivenAnInterruptedSyncRecoveryThatIsBlocked() => first = new SyncRunServices(journal);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private AccountId Personal => first.Personal;

    public void Dispose()
    {
        first.Dispose();

        foreach (var restart in restarts)
            restart.Dispose();
    }

    private SyncRunServices Restart()
    {
        journal.Restart();
        var next = new SyncRunServices(journal, first.Baseline);
        restarts.Add(next);

        return next;
    }

    private async Task LeaveUnfinishedAsync(AccountId accountId) => _ = await journal.StartAsync(accountId, SyncTriggerFactory.CreateScheduled(), CancellationToken);

    [Fact]
    public async Task when_the_account_needs_to_sign_in_then_the_outcome_names_the_blocker_and_the_feed_says_so()
    {
        await LeaveUnfinishedAsync(Personal);
        var next = Restart();
        next.Steps.Index = Result.Failure<RemoteUpdateSummary, ProviderError>(ProviderErrorFactory.CreateAuthenticationRequired());

        var outcomes = await next.Recovery.RecoverAsync(CancellationToken);

        outcomes.ShouldBe([RecoveryOutcomeFactory.CreateNotPossible(Personal, RecoveryBlockerFactory.CreateSignInRequired())]);
        next.ActivityLog.Appended.Select(entry => entry.Kind).ShouldBe([ActivityKindFactory.CreateSyncInterrupted(), ActivityKindFactory.CreateRecoveryBlocked(RecoveryBlockerFactory.CreateSignInRequired())]);
    }

    [Fact]
    public async Task when_the_sync_folder_is_missing_then_the_outcome_names_the_blocker_and_the_feed_says_so()
    {
        await LeaveUnfinishedAsync(Personal);
        var next = Restart();
        next.Steps.Scan = Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateRootUnavailable());

        var outcomes = await next.Recovery.RecoverAsync(CancellationToken);

        outcomes.ShouldBe([RecoveryOutcomeFactory.CreateNotPossible(Personal, RecoveryBlockerFactory.CreateSyncFolderMissing())]);
        next.ActivityLog.Appended.Select(entry => entry.Kind).ShouldBe([ActivityKindFactory.CreateSyncInterrupted(), ActivityKindFactory.CreateRecoveryBlocked(RecoveryBlockerFactory.CreateSyncFolderMissing())]);
    }

    [Fact]
    public async Task when_the_blocker_is_fixed_then_the_next_normal_run_finishes_the_work()
    {
        await LeaveUnfinishedAsync(Personal);
        var next = Restart();
        next.Steps.Index = Result.Failure<RemoteUpdateSummary, ProviderError>(ProviderErrorFactory.CreateAuthenticationRequired());
        _ = await next.Recovery.RecoverAsync(CancellationToken);
        next.Steps.Index = Result.Success<RemoteUpdateSummary, ProviderError>(RemoteUpdateSummaryFactory.Create(RelevantChangeCountsFactory.None, RuleRelocationSummaryFactory.None, Option.None<ResyncKind>(), 1));

        var result = await next.Runner.RunAsync(Personal, SyncTriggerFactory.CreateScheduled(), CancellationToken);

        (result.Match(_ => true, _ => false), journal.Runs.Count(run => run.Outcome.Equals(Option.Some(SyncRunOutcomeFactory.CreateCompleted())))).ShouldBe((true, 1));
    }

    [Fact]
    public async Task when_the_database_failed_its_check_then_nothing_is_marked_resumed_or_told_and_no_pass_runs()
    {
        await LeaveUnfinishedAsync(Personal);
        var next = Restart();
        next.Health.Answer = Result.Failure<Unit, DatabaseHealthFailure>(DatabaseHealthFailureFactory.CreateDamaged());

        var outcomes = await next.Recovery.RecoverAsync(CancellationToken);

        outcomes.ShouldBeEmpty();
        (journal.Runs.Count(run => run.Outcome is Option<SyncRunOutcome>.None), journal.Runs.Count, next.ActivityLog.Calls, next.Steps.Steps.Count).ShouldBe((1, 1, 0, 0));
    }

    private static async Task<T> FinishedAsync<T>(Task<T> task, Microsoft.Extensions.Time.Testing.FakeTimeProvider time)
    {
        while (!task.IsCompleted)
        {
            time.Advance(TimeSpan.FromMilliseconds(250));
            await Task.Delay(5, CancellationToken);
        }

        return await task;
    }

    [Fact]
    public async Task when_the_database_is_sound_again_at_the_next_start_then_the_interrupted_run_is_recovered()
    {
        await LeaveUnfinishedAsync(Personal);
        var damaged = Restart();
        damaged.Health.Answer = Result.Failure<Unit, DatabaseHealthFailure>(DatabaseHealthFailureFactory.CreateUnreadable());
        _ = await FinishedAsync(damaged.Recovery.RecoverAsync(CancellationToken), damaged.Time);
        var repaired = Restart();

        var outcomes = await repaired.Recovery.RecoverAsync(CancellationToken);

        outcomes.ShouldBe([RecoveryOutcomeFactory.CreateResumed(Personal)]);
    }

    [Fact]
    public async Task when_the_database_check_finishes_after_a_run_was_refused_then_recovery_agrees_with_the_run()
    {
        await LeaveUnfinishedAsync(Personal);
        var next = Restart();
        next.Health.Answer = Result.Failure<Unit, DatabaseHealthFailure>(DatabaseHealthFailureFactory.CreateDamaged());
        _ = await next.Runner.RunAsync(Personal, SyncTriggerFactory.CreateManual(), CancellationToken);

        var outcomes = await next.Recovery.RecoverAsync(CancellationToken);

        (outcomes, next.Health.Calls).ShouldBe(([], 1));
    }
}
