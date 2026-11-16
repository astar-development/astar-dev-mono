using System.Data;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Keeps the applied seed version in SQLite, in a single row.</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteSeedStateStore(IDbContextFactory<ClassificationDbContext> dbContextFactory, ILogger<SqliteSeedStateStore> logger) : ISeedStateStore
{
    private const string Location = nameof(SqliteSeedStateStore);

    /// <inheritdoc />
    public Task<Result<Option<string>, ClassificationFailure>> GetAppliedVersionAsync(CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => ReadAsync(cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<Unit, ClassificationFailure>> SetAppliedVersionAsync(string version, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => SaveAsync(version, cancellationToken), cancellationToken);

    private async Task<Result<Option<string>, ClassificationFailure>> ReadAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var state = await dbContext.State.AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<Option<string>, ClassificationFailure>(state is null ? Option.None<string>() : Option.Some(state.SeedVersion));
    }

    private async Task<Result<Unit, ClassificationFailure>> SaveAsync(string version, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var state = await dbContext.State.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (state is null)
            _ = dbContext.State.Add(new ClassificationStateEntity { SeedVersion = version });
        else
            state.SeedVersion = version;

        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<Unit, ClassificationFailure>(Unit.Instance);
    }
}
