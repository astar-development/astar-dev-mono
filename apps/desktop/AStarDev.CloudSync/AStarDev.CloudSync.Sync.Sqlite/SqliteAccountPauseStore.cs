using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the paused accounts in the sync tables: a row means the account is paused. A toggle reads and writes inside one immediate transaction.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports reads and changes that fail, by exception type only.</param>
internal sealed class SqliteAccountPauseStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteAccountPauseStore> logger) : IAccountPauseReader, IAccountPauseWriter, IAccountRemovalParticipant
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AccountId>, ErrorResponse>> GetPausedAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{dbContextFactory}{logger}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<PauseState, ErrorResponse>> ToggleAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId}{cancellationToken}");
}
