using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Accounts.Sqlite;

/// <summary>Clears the EF Core migration lock of the CloudSync SQLite file.</summary>
/// <param name="dbContextFactory">Creates the context that runs the statement.</param>
/// <param name="databaseFile">The SQLite database file.</param>
/// <param name="logger">Reports a failure.</param>
internal sealed class SqliteMigrationLockClearer(IDbContextFactory<CloudSyncDbContext> dbContextFactory, IFileInfo databaseFile, ILogger<SqliteMigrationLockClearer> logger) : IMigrationLockClearer
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> ClearAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{dbContextFactory} {databaseFile} {logger} {cancellationToken}");
}
