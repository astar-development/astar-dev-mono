using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Stores the status each account's last sync run left in the SyncOutcomes table, one row per account.</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="time">Stamps each save.</param>
/// <param name="logger">Logs the type of a failure, never its message.</param>
internal sealed class SqliteSyncOutcomeStore(IDbContextFactory<SyncDbContext> dbContextFactory, TimeProvider time, ILogger<SqliteSyncOutcomeStore> logger) : ISyncOutcomeReader, ISyncOutcomeWriter, IAccountRemovalParticipant
{
    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public Task<Result<IReadOnlyDictionary<AccountId, SyncStatus>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{dbContextFactory}{time}{logger}{cancellationToken}{Changed}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> SaveAsync(AccountId accountId, SyncStatus status, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{accountId}{status}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{accountId}{cancellationToken}");
}
