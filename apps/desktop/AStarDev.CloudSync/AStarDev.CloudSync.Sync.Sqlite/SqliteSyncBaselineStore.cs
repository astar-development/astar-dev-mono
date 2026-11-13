using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps an account's sync baseline in SQLite (#1399).</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteSyncBaselineStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteSyncBaselineStore> logger) : ISyncBaselineReader, ISyncBaselineWriter, IAccountRemovalParticipant
{
    /// <inheritdoc />
    public Task<Result<Option<SyncBaselineEntry>, ErrorResponse>> GetAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) => throw new NotImplementedException($"{dbContextFactory} {logger} {accountId} {path} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<SyncBaselineEntry>, ErrorResponse>> GetUnderAsync(AccountId accountId, RelativeSyncPath under, Option<RelativeSyncPath> after, int limit, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId} {under} {after} {limit} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> UpsertAsync(AccountId accountId, IReadOnlyList<SyncBaselineEntry> entries, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId} {entries} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, IReadOnlyList<RelativeSyncPath> paths, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId} {paths} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId} {cancellationToken}");
}
