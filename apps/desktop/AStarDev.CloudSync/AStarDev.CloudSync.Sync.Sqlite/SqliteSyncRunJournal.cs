using System.Data;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the run journal in the sync tables, newest 100 runs per account. A finish stores and prunes inside one serializable transaction. A run still in progress is never pruned.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="timeProvider">Stamps starts and ends.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteSyncRunJournal(IDbContextFactory<SyncDbContext> dbContextFactory, TimeProvider timeProvider, ILogger<SqliteSyncRunJournal> logger) : ISyncRunJournal, IAccountRemovalParticipant
{
    private const string Location = nameof(SqliteSyncRunJournal);

    /// <inheritdoc />
    public Task<Result<SyncRunId, ErrorResponse>> StartAsync(AccountId accountId, SyncTrigger trigger, CancellationToken cancellationToken) =>
        Try.RunAsync(() => InsertAsync(accountId, trigger, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<SyncRunId, ErrorResponse>, exception => Failed<SyncRunId>(exception, "The sync run could not be recorded."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> FinishAsync(SyncRunId runId, SyncRunOutcome outcome, CancellationToken cancellationToken) =>
        Try.RunAsync(() => UpdateAsync(runId, outcome, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The end of the sync run could not be recorded."));

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<InterruptedSyncRun>, ErrorResponse>> GetInterruptedAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadInterruptedAsync(cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<IReadOnlyList<InterruptedSyncRun>, ErrorResponse>, exception => Failed<IReadOnlyList<InterruptedSyncRun>>(exception, "The interrupted sync runs could not be read."));

    /// <inheritdoc />
    public Task<Result<Option<DateTimeOffset>, ErrorResponse>> GetLastSyncAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadLastSyncAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Option<DateTimeOffset>, ErrorResponse>, exception => Failed<Option<DateTimeOffset>>(exception, "The last sync could not be read."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The account's sync runs could not be removed."));

    private static string NameOf(SyncRunOutcome outcome) => outcome switch
    {
        SyncRunOutcome.Completed => nameof(SyncRunOutcome.Completed),
        SyncRunOutcome.Cancelled => nameof(SyncRunOutcome.Cancelled),
        SyncRunOutcome.Failed => nameof(SyncRunOutcome.Failed),
        _ => nameof(SyncRunOutcome.Interrupted),
    };

    private static string? ReasonOf(SyncRunOutcome outcome) => outcome is SyncRunOutcome.Failed failed ? failed.Reason.GetType().Name : null;

    private static async Task PruneAsync(SyncDbContext dbContext, Guid accountId, CancellationToken cancellationToken)
    {
        var oldestKept = await dbContext.SyncRuns
            .Where(run => run.AccountId == accountId)
            .OrderByDescending(run => run.StartedAtUtcTicks)
            .Skip(SyncRunJournalLimits.RunsPerAccount - 1)
            .Select(run => (long?)run.StartedAtUtcTicks)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (oldestKept is { } ticks)
            _ = await dbContext.SyncRuns.Where(run => run.AccountId == accountId && run.FinishedAtUtcTicks != null && run.StartedAtUtcTicks < ticks).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<SyncRunId> InsertAsync(AccountId accountId, SyncTrigger trigger, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var id = Guid.NewGuid();
        _ = dbContext.SyncRuns.Add(new SyncRunEntity { Id = id, AccountId = accountId.Value, Trigger = trigger.GetType().Name, StartedAtUtcTicks = timeProvider.GetUtcNow().UtcTicks });
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SyncRunId(id);
    }

    private async Task<Unit> UpdateAsync(SyncRunId runId, SyncRunOutcome outcome, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var run = await dbContext.SyncRuns.FirstOrDefaultAsync(row => row.Id == runId.Value && row.FinishedAtUtcTicks == null, cancellationToken).ConfigureAwait(false);
        if (run is null)
            return Unit.Instance;

        run.FinishedAtUtcTicks = timeProvider.GetUtcNow().UtcTicks;
        run.Outcome = NameOf(outcome);
        run.FailureReason = ReasonOf(outcome);
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PruneAsync(dbContext, run.AccountId, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private async Task<IReadOnlyList<InterruptedSyncRun>> ReadInterruptedAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await dbContext.SyncRuns.AsNoTracking().Where(run => run.FinishedAtUtcTicks == null).OrderBy(run => run.StartedAtUtcTicks).ToListAsync(cancellationToken).ConfigureAwait(false);

        return [.. rows.Select(run => InterruptedSyncRunFactory.Create(new SyncRunId(run.Id), AccountIdFactory.Create(run.AccountId)))];
    }

    private async Task<Option<DateTimeOffset>> ReadLastSyncAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var completed = nameof(SyncRunOutcome.Completed);
        var ticks = await dbContext.SyncRuns.AsNoTracking()
            .Where(run => run.AccountId == accountId.Value && run.Outcome == completed)
            .OrderByDescending(run => run.FinishedAtUtcTicks)
            .Select(run => run.FinishedAtUtcTicks)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return ticks is { } finished ? Option.Some(new DateTimeOffset(finished, TimeSpan.Zero)) : Option.None<DateTimeOffset>();
    }

    private async Task<Unit> DeleteAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.SyncRuns.Where(run => run.AccountId == accountId.Value).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Result<T, ErrorResponse> Failed<T>(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<T, ErrorResponse>(new ErrorResponse(message));
    }
}
