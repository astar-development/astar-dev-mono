using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Keeps one row per sync run so a run the app died in can be found at the next start, and so the last sync survives a restart (#1610). It keeps the newest <see cref="SyncRunJournalLimits.RunsPerAccount"/> runs per account.</summary>
public interface ISyncRunJournal
{
    /// <summary>Records that a run started.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="trigger">What started the run.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<SyncRunId, ErrorResponse>> StartAsync(AccountId accountId, SyncTrigger trigger, CancellationToken cancellationToken);

    /// <summary>Records how a run ended and drops the account's runs beyond the history limit. Finishing an unknown or already finished run changes nothing.</summary>
    /// <param name="runId">The run.</param>
    /// <param name="outcome">How it ended.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<Unit, ErrorResponse>> FinishAsync(SyncRunId runId, SyncRunOutcome outcome, CancellationToken cancellationToken);

    /// <summary>The runs that started and never finished. Only call it before this app session starts any run: a run in progress now is not interrupted.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyList<InterruptedSyncRun>, ErrorResponse>> GetInterruptedAsync(CancellationToken cancellationToken);

    /// <summary>When the account's newest completed run finished, if one did.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<Option<DateTimeOffset>, ErrorResponse>> GetLastSyncAsync(AccountId accountId, CancellationToken cancellationToken);
}
