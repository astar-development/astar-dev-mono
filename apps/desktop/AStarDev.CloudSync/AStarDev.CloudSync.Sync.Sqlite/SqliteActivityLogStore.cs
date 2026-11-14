using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the activity log in the sync tables, newest 1,000 entries per account.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteActivityLogStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteActivityLogStore> logger) : IActivityLogReader, IActivityLogWriter, IAccountRemovalParticipant
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> AppendAsync(IReadOnlyList<ActivityEntry> entries, CancellationToken cancellationToken) => throw new NotImplementedException($"{dbContextFactory}{logger}{entries}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ActivityEntry>, ErrorResponse>> GetRecentAsync(Option<AccountId> accountId, int count, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId}{count}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId}{cancellationToken}");
}
