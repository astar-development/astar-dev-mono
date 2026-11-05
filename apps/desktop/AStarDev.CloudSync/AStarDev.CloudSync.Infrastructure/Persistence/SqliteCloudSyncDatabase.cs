using System.IO.Abstractions;
using AStarDev.CloudSync.Core.Persistence;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
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
    public async Task<Result<Unit, ErrorResponse>> MigrateAsync(CancellationToken cancellationToken)
    {
        try
        {
            databaseFile.Directory?.Create();
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

            return Unit.Instance;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogMessage.Error(logger, "Could not create or migrate the CloudSync database.", exception);

            return new ErrorResponse("The CloudSync database could not be created or migrated.");
        }
    }
}
