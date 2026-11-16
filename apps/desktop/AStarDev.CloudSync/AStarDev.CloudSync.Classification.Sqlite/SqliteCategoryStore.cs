using System.Data;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Keeps the classification categories in SQLite. An upsert runs in one serializable transaction, so concurrent upserts of one name leave one row.</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteCategoryStore(IDbContextFactory<ClassificationDbContext> dbContextFactory, ILogger<SqliteCategoryStore> logger) : ICategoryStore
{
    private const string Location = nameof(SqliteCategoryStore);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ClassificationCategory>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => ReadAllAsync(cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<ClassificationCategory, ClassificationFailure>> UpsertAsync(ClassificationCategory category, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => SaveAsync(category, cancellationToken), cancellationToken);

    private async Task<Result<IReadOnlyList<ClassificationCategory>, ClassificationFailure>> ReadAllAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var entities = await dbContext.Categories.AsNoTracking().OrderBy(category => category.NameKey).ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<ClassificationCategory>, ClassificationFailure>([.. entities.Select(TermMapping.ToCategory)]);
    }

    private async Task<Result<ClassificationCategory, ClassificationFailure>> SaveAsync(ClassificationCategory category, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var key = category.Name.ToUpperInvariant();
        var stored = await dbContext.Categories.FirstOrDefaultAsync(item => item.NameKey == key, cancellationToken).ConfigureAwait(false);
        if (stored is null)
        {
            stored = new CategoryEntity { Id = category.Id.Value, Name = category.Name };
            _ = dbContext.Categories.Add(stored);
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success<ClassificationCategory, ClassificationFailure>(TermMapping.ToCategory(stored));
    }
}
