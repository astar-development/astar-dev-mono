using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Migrates the classification tables after the database registered before it. The classification migration only runs when the earlier one succeeded, because that one creates the database folder and file.</summary>
/// <param name="previous">The migration that runs first, or none when nothing was registered before.</param>
/// <param name="dbContextFactory">Creates the context that runs the migrations.</param>
/// <param name="logger">Reports a migration that fails.</param>
internal sealed class ClassificationDatabase(Option<ICloudSyncDatabase> previous, IDbContextFactory<ClassificationDbContext> dbContextFactory, ILogger<ClassificationDatabase> logger) : ICloudSyncDatabase
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> MigrateAsync(CancellationToken cancellationToken) =>
        previous.Match(first => first.MigrateAsync(cancellationToken), () => Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance)))
            .BindAsync(_ => ApplyMigrationsAsync(cancellationToken));

    private Task<Result<Unit, ErrorResponse>> ApplyMigrationsAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => MigrateContextAsync(cancellationToken), cancellationToken).MatchAsync(Result.Success<Unit, ErrorResponse>, Failed);

    private async Task<Unit> MigrateContextAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Result<Unit, ErrorResponse> Failed(Exception exception)
    {
        LogMessage.Error(logger, "The classification tables could not be migrated.", exception.ErrorType());

        return Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The classification tables could not be migrated."));
    }
}
