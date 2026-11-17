using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Keeps the last completed reclassify run in SQLite, in a single row.</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteReclassifyRunStore(IDbContextFactory<ClassificationDbContext> dbContextFactory, ILogger<SqliteReclassifyRunStore> logger) : IReclassifyRunStore
{
    /// <inheritdoc />
    public Task<Result<Option<ReclassifyRunRecord>, ClassificationFailure>> GetLastCompletedAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{dbContextFactory}{logger}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ClassificationFailure>> SetLastCompletedAsync(ReclassifyRunRecord run, CancellationToken cancellationToken) => throw new NotImplementedException($"{run}{cancellationToken}");
}
