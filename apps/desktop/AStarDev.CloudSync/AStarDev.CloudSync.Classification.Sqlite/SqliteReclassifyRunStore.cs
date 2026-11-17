using System.Data;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Keeps the last completed reclassify run in SQLite, in a single row.</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteReclassifyRunStore(IDbContextFactory<ClassificationDbContext> dbContextFactory, ILogger<SqliteReclassifyRunStore> logger) : IReclassifyRunStore
{
    private const string Location = nameof(SqliteReclassifyRunStore);

    /// <inheritdoc />
    public Task<Result<Option<ReclassifyRunRecord>, ClassificationFailure>> GetLastCompletedAsync(CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => ReadAsync(cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<Unit, ClassificationFailure>> SetLastCompletedAsync(ReclassifyRunRecord run, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => SaveAsync(run, cancellationToken), cancellationToken);

    private async Task<Result<Option<ReclassifyRunRecord>, ClassificationFailure>> ReadAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var run = await dbContext.ReclassifyRuns.AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<Option<ReclassifyRunRecord>, ClassificationFailure>(run is null ? Option.None<ReclassifyRunRecord>() : Option.Some(ToRecord(run)));
    }

    private async Task<Result<Unit, ClassificationFailure>> SaveAsync(ReclassifyRunRecord run, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var stored = await dbContext.ReclassifyRuns.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (stored is null)
        {
            stored = new ReclassifyRunEntity();
            _ = dbContext.ReclassifyRuns.Add(stored);
        }

        stored.EngineVersion = run.EngineVersion;
        stored.SeedVersion = run.SeedVersion.Match(version => (string?)version, () => null);
        stored.FinishedAt = run.FinishedAt;
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<Unit, ClassificationFailure>(Unit.Instance);
    }

    private static ReclassifyRunRecord ToRecord(ReclassifyRunEntity run) => ReclassifyRunRecordFactory.Create(run.EngineVersion, run.SeedVersion is null ? Option.None<string>() : Option.Some(run.SeedVersion), run.FinishedAt);
}
