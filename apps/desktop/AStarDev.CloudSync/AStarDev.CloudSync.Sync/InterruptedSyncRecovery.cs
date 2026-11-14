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
/// <param name="timeProvider">Supplies the clock and the delays.</param>
/// <param name="logger">Reports what could not be read or written, and how many runs were interrupted, without account details.</param>
internal sealed class InterruptedSyncRecovery(ISyncRunJournal journal, IActivityLogWriter activity, IStaggerPolicy stagger, ISyncPassRunner runner, TimeProvider timeProvider, ILogger<InterruptedSyncRecovery> logger) : IInterruptedSyncRecovery
{
    private const string Location = nameof(InterruptedSyncRecovery);

    /// <inheritdoc />
    public Task<IReadOnlyList<RecoveryOutcome>> RecoverAsync(CancellationToken cancellationToken) =>
        journal.GetInterruptedAsync(cancellationToken).MatchAsync(runs => RecoverRunsAsync(runs, cancellationToken), _ => NothingRecovered());

    private static RecoveryOutcome Resumed(AccountId accountId, Result<SyncPassSummary, SyncPassFailure> result) =>
        result.Match(_ => RecoveryOutcomeFactory.CreateResumed(accountId), failure => RecoveryOutcomeFactory.CreateNotResumed(accountId, failure));

    private Task<IReadOnlyList<RecoveryOutcome>> NothingRecovered()
    {
        LogMessage.Warning(logger, Location, "The run journal could not be read, so no interrupted sync was recovered.");

        return Task.FromResult<IReadOnlyList<RecoveryOutcome>>([]);
    }

    private async Task<IReadOnlyList<RecoveryOutcome>> RecoverRunsAsync(IReadOnlyList<InterruptedSyncRun> runs, CancellationToken cancellationToken)
    {
        if (runs.Count == 0)
            return [];

        var accounts = runs.Select(run => run.AccountId).Distinct().ToList();
        var message = $"Found {runs.Count} interrupted sync runs on {accounts.Count} accounts.";
        LogMessage.Information(logger, Location, message);
        foreach (var run in runs)
            _ = await journal.FinishAsync(run.Id, SyncRunOutcomeFactory.CreateInterrupted(), cancellationToken).MatchAsync(done => done, _ => Warn("An interrupted run could not be marked.")).ConfigureAwait(false);

        _ = await TellUserAsync(accounts, cancellationToken).ConfigureAwait(false);
        var offsets = stagger.OffsetsFor(accounts, RecoveryStagger.Window);

        return await Task.WhenAll(accounts.Select(account => ResumeAsync(account, offsets[account], cancellationToken))).ConfigureAwait(false);
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
        await ((Task)waiting).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (waiting.IsCanceled)
            return RecoveryOutcomeFactory.CreateNotResumed(accountId, SyncPassFailureFactory.CreateCancelled());

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
