using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Writes a sync run's start and end to the run journal (#1610). A journal that cannot be written is warned about and never fails or blocks the run: the run then has no row, so a crash during it is not recovered.</summary>
/// <param name="journal">Stores the rows.</param>
/// <param name="live">Remembers the rows this session is working on.</param>
/// <param name="logger">Reports a write that failed, without details.</param>
internal sealed class SyncPassJournal(ISyncRunJournal journal, LiveSyncRuns live, ILogger<SyncPassJournal> logger)
{
    private const string Location = nameof(SyncPassJournal);

    /// <summary>Records that a run started. The write is not cancellable: a run the user cancels at once still earns its row.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="trigger">What started the run.</param>
    public Task<Option<SyncRunId>> StartAsync(AccountId accountId, SyncTrigger trigger) =>
        live.StartingAsync(() => journal.StartAsync(accountId, trigger, CancellationToken.None).MatchAsync(Began, _ => NoRow()));

    /// <summary>Records how the run ended. Nothing is written for a run that has no row.</summary>
    /// <param name="runId">The run's row, if it was written.</param>
    /// <param name="outcome">What the run did.</param>
    public Task<Unit> FinishAsync(Option<SyncRunId> runId, Result<SyncPassSummary, SyncPassFailure> outcome) =>
        runId.Match(
            id => journal.FinishAsync(id, OutcomeOf(outcome), CancellationToken.None).MatchAsync(_ => Ended(id), _ => EndedWithWarning(id)),
            () => Task.FromResult(Unit.Instance));

    private static SyncRunOutcome OutcomeOf(Result<SyncPassSummary, SyncPassFailure> outcome) => outcome.Match(_ => SyncRunOutcomeFactory.CreateCompleted(), FromFailure);

    private static SyncRunOutcome FromFailure(SyncPassFailure failure) => failure switch
    {
        SyncPassFailure.Cancelled => SyncRunOutcomeFactory.CreateCancelled(),
        SyncPassFailure.RootUnavailable => SyncRunOutcomeFactory.CreateFailed(SyncFailureReasonFactory.CreateRootUnavailable()),
        SyncPassFailure.QuotaExceeded => SyncRunOutcomeFactory.CreateFailed(SyncFailureReasonFactory.CreateQuotaExceeded()),
        SyncPassFailure.InsufficientDiskSpace => SyncRunOutcomeFactory.CreateFailed(SyncFailureReasonFactory.CreateInsufficientDiskSpace()),
        _ => SyncRunOutcomeFactory.CreateFailed(SyncFailureReasonFactory.CreateUnexpected()),
    };

    private Option<SyncRunId> Began(SyncRunId id)
    {
        live.Add(id);

        return Option.Some(id);
    }

    private Unit Ended(SyncRunId id)
    {
        live.Remove(id);

        return Unit.Instance;
    }

    private Unit EndedWithWarning(SyncRunId id)
    {
        live.Remove(id);

        return Warn();
    }

    private Option<SyncRunId> NoRow()
    {
        _ = Warn();

        return Option.None<SyncRunId>();
    }

    private Unit Warn()
    {
        LogMessage.Warning(logger, Location, "The sync run journal could not be written.");

        return Unit.Instance;
    }
}
