using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the sync trash retention in one row of the sync tables. No row, or a value outside the allowed range, means the default.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports reads and saves that fail, by exception type only.</param>
internal sealed class SqliteTrashRetentionStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteTrashRetentionStore> logger) : ITrashRetentionSettings
{
    /// <inheritdoc />
    public Task<Result<LocalTrashOptions, ErrorResponse>> GetAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{dbContextFactory}{logger}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<LocalTrashOptions, ErrorResponse>> SetAsync(LocalTrashOptions options, CancellationToken cancellationToken) => throw new NotImplementedException($"{options}{cancellationToken}");
}
