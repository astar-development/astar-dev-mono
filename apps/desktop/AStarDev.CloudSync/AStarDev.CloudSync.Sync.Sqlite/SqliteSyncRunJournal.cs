using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the run journal in the sync tables.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="timeProvider">Stamps starts and ends.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteSyncRunJournal(IDbContextFactory<SyncDbContext> dbContextFactory, TimeProvider timeProvider, ILogger<SqliteSyncRunJournal> logger) : ISyncRunJournal, IAccountRemovalParticipant
{
    /// <inheritdoc />
    public Task<Result<SyncRunId, ErrorResponse>> StartAsync(AccountId accountId, SyncTrigger trigger, CancellationToken cancellationToken) => throw new NotImplementedException($"{dbContextFactory}{timeProvider}{logger}{accountId}{trigger}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> FinishAsync(SyncRunId runId, SyncRunOutcome outcome, CancellationToken cancellationToken) => throw new NotImplementedException($"{runId}{outcome}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<InterruptedSyncRun>, ErrorResponse>> GetInterruptedAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Option<DateTimeOffset>, ErrorResponse>> GetLastSyncAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId}{cancellationToken}");
}
