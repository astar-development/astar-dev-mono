using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the app-wide automatic sync schedule in one row of the sync tables. No row means the default schedule; a row without minutes means manual only.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports reads and saves that fail, by exception type only.</param>
internal sealed class SqliteSyncScheduleStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteSyncScheduleStore> logger) : ISyncScheduleSettings
{
    /// <inheritdoc />
    public Task<Result<SyncSchedule, ErrorResponse>> GetAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{dbContextFactory}{logger}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<SyncSchedule, ErrorResponse>> SetAsync(SyncSchedule schedule, CancellationToken cancellationToken) => throw new NotImplementedException($"{schedule}{cancellationToken}");
}
