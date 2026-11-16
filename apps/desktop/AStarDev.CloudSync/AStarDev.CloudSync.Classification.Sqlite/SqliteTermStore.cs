using System.Data;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Keeps the classification terms in SQLite. A write runs in one serializable transaction, so concurrent upserts of one term leave one row, and a term is never deleted.</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteTermStore(IDbContextFactory<ClassificationDbContext> dbContextFactory, ILogger<SqliteTermStore> logger) : ITermStore
{
    private const string Location = nameof(SqliteTermStore);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => ReadAllAsync(cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<ClassificationTerm, ClassificationFailure>> UpsertAsync(ClassificationTerm term, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => SaveAsync(term, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> UpsertManyAsync(IReadOnlyList<ClassificationTerm> terms, CancellationToken cancellationToken) => throw new NotImplementedException($"{terms.Count} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Option<ClassificationTerm>, ClassificationFailure>> GetByIdAsync(TermId termId, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => ReadOneAsync(termId, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<ClassificationTerm, ClassificationFailure>> SetStatusAsync(TermId termId, TermStatus status, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => ChangeStatusAsync(termId, status, cancellationToken), cancellationToken);

    private async Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> ReadAllAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var entities = await dbContext.Terms.AsNoTracking().OrderBy(term => term.Text).ThenBy(term => term.CategoryId).ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<ClassificationTerm>, ClassificationFailure>([.. entities.Select(TermMapping.ToTerm)]);
    }

    private async Task<Result<Option<ClassificationTerm>, ClassificationFailure>> ReadOneAsync(TermId termId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var entity = await dbContext.Terms.AsNoTracking().FirstOrDefaultAsync(term => term.Id == termId.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success<Option<ClassificationTerm>, ClassificationFailure>(entity is null ? Option.None<ClassificationTerm>() : Option.Some(TermMapping.ToTerm(entity)));
    }

    private async Task<Result<ClassificationTerm, ClassificationFailure>> SaveAsync(ClassificationTerm term, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var categoryId = term.CategoryId.Value;
        if (!await dbContext.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken).ConfigureAwait(false))
            return Result.Failure<ClassificationTerm, ClassificationFailure>(ClassificationFailureFactory.CreateNotFound());

        var stored = await dbContext.Terms.FirstOrDefaultAsync(item => item.CategoryId == categoryId && item.Text == term.Text, cancellationToken).ConfigureAwait(false);
        if (stored is null)
        {
            stored = TermMapping.ToEntity(term);
            _ = dbContext.Terms.Add(stored);
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success<ClassificationTerm, ClassificationFailure>(TermMapping.ToTerm(stored));
    }

    private async Task<Result<ClassificationTerm, ClassificationFailure>> ChangeStatusAsync(TermId termId, TermStatus status, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var stored = await dbContext.Terms.FirstOrDefaultAsync(item => item.Id == termId.Value, cancellationToken).ConfigureAwait(false);
        if (stored is null)
            return Result.Failure<ClassificationTerm, ClassificationFailure>(ClassificationFailureFactory.CreateNotFound());

        stored.IsIgnored = status is TermStatus.Ignored;
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<ClassificationTerm, ClassificationFailure>(TermMapping.ToTerm(stored));
    }
}
