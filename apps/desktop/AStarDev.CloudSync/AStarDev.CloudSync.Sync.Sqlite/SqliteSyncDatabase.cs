using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Migrates the sync tables. The accounts migration runs first and creates the database folder and file.</summary>
/// <param name="dbContextFactory">Creates the context that runs the migrations.</param>
/// <param name="logger">Reports a migration that fails.</param>
internal sealed class SqliteSyncDatabase(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteSyncDatabase> logger) : ICloudSyncDatabase
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> MigrateAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => ApplyMigrationsAsync(cancellationToken), cancellationToken).MatchAsync(Result.Success<Unit, ErrorResponse>, Failed);

    private async Task<Unit> ApplyMigrationsAsync(CancellationToken cancellationToken)
    {
        await using var syncContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await syncContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Result<Unit, ErrorResponse> Failed(Exception exception)
    {
        LogMessage.Error(logger, "The sync tables could not be migrated.", exception.ErrorType());

        return Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The sync tables could not be migrated."));
    }
}
