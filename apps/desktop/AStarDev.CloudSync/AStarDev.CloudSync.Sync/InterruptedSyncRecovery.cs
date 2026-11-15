using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="journal">Finds and marks the interrupted runs.</param>
/// <param name="activity">Tells the user a run was interrupted.</param>
/// <param name="stagger">Spreads the recovery passes.</param>
/// <param name="runner">Runs the recovery passes.</param>
/// <param name="checks">Tells which accounts are paused and whether the database is sound.</param>
/// <param name="timeProvider">Supplies the clock and the delays.</param>
/// <param name="logger">Reports what could not be read or written, and how many runs were interrupted, without account details.</param>
internal sealed class InterruptedSyncRecovery(ISyncRunJournal journal, IActivityLogWriter activity, IStaggerPolicy stagger, ISyncPassRunner runner, RecoveryChecks checks, TimeProvider timeProvider, ILogger<InterruptedSyncRecovery> logger) : IInterruptedSyncRecovery
{
    private const string Location = nameof(InterruptedSyncRecovery);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecoveryOutcome>> RecoverAsync(CancellationToken cancellationToken)
    {
        var health = await checks.Health.CheckAsync(cancellationToken).ConfigureAwait(false);
        if (health is not DatabaseHealthStatus.Healthy)
            return DatabaseNotSound(health);

        var (read, liveRuns) = await checks.Live.ReadingAsync(() => journal.GetInterruptedAsync(cancellationToken), cancellationToken).ConfigureAwait(false);

        return await read.Match(runs => RecoverRunsAsync(runs, liveRuns, cancellationToken), _ => NothingRecovered()).ConfigureAwait(false);
    }

    private static RecoveryOutcome Resumed(AccountId accountId, Result<SyncPassSummary, SyncPassFailure> result) =>
        result.Match(_ => RecoveryOutcomeFactory.CreateResumed(accountId), failure => Stopped(accountId, failure));

    private static RecoveryOutcome Stopped(AccountId accountId, SyncPassFailure failure) => failure switch
    {
        SyncPassFailure.AuthenticationRequired => RecoveryOutcomeFactory.CreateNotPossible(accountId, RecoveryBlockerFactory.CreateSignInRequired()),
        SyncPassFailure.RootUnavailable => RecoveryOutcomeFactory.CreateNotPossible(accountId, RecoveryBlockerFactory.CreateSyncFolderMissing()),
        _ => RecoveryOutcomeFactory.CreateNotResumed(accountId, failure),
    };

    private IReadOnlyList<RecoveryOutcome> DatabaseNotSound(DatabaseHealthStatus health)
    {
        if (health is DatabaseHealthStatus.Failed)
            LogMessage.Warning(logger, Location, "The database failed its integrity check, so no interrupted sync was recovered. The interrupted runs stay as they are until the next start.");

        return [];
    }

    private Task<IReadOnlyList<RecoveryOutcome>> NothingRecovered()
    {
        LogMessage.Warning(logger, Location, "The run journal could not be read, so no interrupted sync was recovered.");

        return Task.FromResult<IReadOnlyList<RecoveryOutcome>>([]);
    }

    private async Task<IReadOnlyList<RecoveryOutcome>> RecoverRunsAsync(IReadOnlyList<InterruptedSyncRun> unfinished, IReadOnlySet<SyncRunId> liveRuns, CancellationToken cancellationToken)
    {
        var runs = unfinished.Where(run => !liveRuns.Contains(run.Id)).ToList();
        if (runs.Count == 0)
            return [];

        var accounts = runs.Select(run => run.AccountId).Distinct().ToList();
        var message = $"Found {runs.Count} interrupted sync runs on {accounts.Count} accounts.";
        LogMessage.Information(logger, Location, message);
        foreach (var run in runs)
            _ = await journal.FinishAsync(run.Id, SyncRunOutcomeFactory.CreateInterrupted(), cancellationToken).MatchAsync(done => done, _ => Warn("An interrupted run could not be marked.")).ConfigureAwait(false);

        _ = await TellUserAsync(accounts, cancellationToken).ConfigureAwait(false);
        var pausedIds = await checks.Paused.GetPausedAsync(cancellationToken).MatchAsync(found => found, _ => HoldAll(accounts)).ConfigureAwait(false);
        var resumable = accounts.Where(account => !pausedIds.Contains(account)).ToList();
        var offsets = stagger.OffsetsFor(resumable, RecoveryStagger.Window);
        var held = accounts.Where(pausedIds.Contains).Select(RecoveryOutcomeFactory.CreateHeld);
        var resumed = await Task.WhenAll(resumable.Select(account => ResumeAsync(account, offsets[account], cancellationToken))).ConfigureAwait(false);
        _ = await TellBlockedAsync(resumed, cancellationToken).ConfigureAwait(false);

        return [.. held, .. resumed];
    }

    private Task<Unit> TellBlockedAsync(RecoveryOutcome[] outcomes, CancellationToken cancellationToken)
    {
        var occurredAt = timeProvider.GetUtcNow();
        IReadOnlyList<ActivityEntry> entries = [.. outcomes.OfType<RecoveryOutcome.NotPossible>().Select(blocked => ActivityEntryFactory.Create(blocked.AccountId, Option.None<SyncPairId>(), ActivityKindFactory.CreateRecoveryBlocked(blocked.Blocker), occurredAt))];

        return entries.Count == 0 ? Task.FromResult(Unit.Instance) : activity.AppendAsync(entries, cancellationToken).MatchAsync(done => done, _ => Warn("The blocked recovery could not be written to the activity log."));
    }

    private List<AccountId> HoldAll(List<AccountId> accounts)
    {
        LogMessage.Warning(logger, Location, "The paused accounts could not be read, so no interrupted sync was resumed.");

        return accounts;
    }

    private Task<Unit> TellUserAsync(List<AccountId> accounts, CancellationToken cancellationToken)
    {
        var occurredAt = timeProvider.GetUtcNow();
        IReadOnlyList<ActivityEntry> entries = [.. accounts.Select(account => ActivityEntryFactory.Create(account, Option.None<SyncPairId>(), ActivityKindFactory.CreateSyncInterrupted(), occurredAt))];

        return activity.AppendAsync(entries, cancellationToken).MatchAsync(done => done, _ => Warn("The interrupted sync could not be written to the activity log."));
    }

    private async Task<RecoveryOutcome> ResumeAsync(AccountId accountId, TimeSpan offset, CancellationToken cancellationToken)
    {
        var waiting = Task.Delay(offset, timeProvider, cancellationToken);
        await waiting.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (waiting.IsCanceled)
            return RecoveryOutcomeFactory.CreateNotResumed(accountId, SyncPassFailureFactory.CreateCancelled());

        var stillPaused = await checks.Paused.GetPausedAsync(cancellationToken).MatchAsync(found => found.Contains(accountId), _ => true).ConfigureAwait(false);
        if (stillPaused)
            return RecoveryOutcomeFactory.CreateHeld(accountId);

        return await Try.RunAsync(() => runner.RunAsync(accountId, SyncTriggerFactory.CreateRecovery(), cancellationToken), cancellationToken)
            .MatchAsync(result => Resumed(accountId, result), _ => RecoveryOutcomeFactory.CreateNotResumed(accountId, SyncPassFailureFactory.CreateUnexpected()))
            .ConfigureAwait(false);
    }

    private Unit Warn(string message)
    {
        LogMessage.Warning(logger, Location, message);

        return Unit.Instance;
    }
}
