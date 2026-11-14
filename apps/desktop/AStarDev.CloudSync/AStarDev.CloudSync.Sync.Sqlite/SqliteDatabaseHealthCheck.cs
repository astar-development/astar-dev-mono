using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <inheritdoc />
/// <param name="databaseFile">The CloudSync database file.</param>
/// <param name="logger">Reports a damaged or unreadable database, with the exception type only.</param>
internal sealed class SqliteDatabaseHealthCheck(IFileInfo databaseFile, ILogger<SqliteDatabaseHealthCheck> logger) : IDatabaseHealthCheck
{
    /// <inheritdoc />
    public Task<Result<Unit, DatabaseHealthFailure>> CheckAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{databaseFile} {logger} {cancellationToken}");
}
