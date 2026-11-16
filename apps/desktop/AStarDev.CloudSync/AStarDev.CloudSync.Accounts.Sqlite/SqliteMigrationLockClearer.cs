using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
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
        databaseFile.Exists
            ? Try.RunAsync(() => DropLockAsync(cancellationToken), cancellationToken).MatchAsync(Result.Success<Unit, ErrorResponse>, Failed)
            : Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));

    private async Task<Unit> DropLockAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"__EFMigrationsLock\"", cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Result<Unit, ErrorResponse> Failed(Exception exception)
    {
        LogMessage.Error(logger, "The migration lock could not be cleared.", exception.ErrorType());

        return Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The migration lock could not be cleared."));
    }
}
