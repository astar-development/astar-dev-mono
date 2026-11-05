using System.IO.Abstractions;
using AStarDev.CloudSync.Core.Persistence;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Infrastructure.Persistence;

/// <summary>Creates and migrates the CloudSync SQLite database file.</summary>
/// <param name="dbContextFactory">Creates the context that runs the migrations.</param>
/// <param name="databaseFile">The SQLite database file; its folder is created when missing.</param>
/// <param name="logger">Reports a migration that fails.</param>
public sealed class SqliteCloudSyncDatabase(IDbContextFactory<CloudSyncDbContext> dbContextFactory, IFileInfo databaseFile, ILogger<SqliteCloudSyncDatabase> logger) : ICloudSyncDatabase
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> MigrateAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{dbContextFactory}{databaseFile}{logger}");
}
