using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps each account's log level in the sync tables (#1411). An account with no row is standard. Removing the account removes its row.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="levels">The running logging, told about every change.</param>
/// <param name="logger">Reports reads and changes that fail, by exception type only.</param>
internal sealed class SqliteAccountLogLevelStore(IDbContextFactory<SyncDbContext> dbContextFactory, AccountLogLevels levels, ILogger<SqliteAccountLogLevelStore> logger) : IAccountLogLevelSettings, IAccountRemovalParticipant
{
    /// <inheritdoc />
    public Task<Result<AccountLogLevel, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{dbContextFactory} {levels} {logger} {accountId} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> SetAsync(AccountId accountId, AccountLogLevel level, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId} {level} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AccountId>, ErrorResponse>> ListVerboseAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId} {cancellationToken}");
}
