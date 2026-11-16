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
    private const int TransactionSize = 1000;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => ReadAllAsync(cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<ClassificationTerm, ClassificationFailure>> UpsertAsync(ClassificationTerm term, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => SaveAsync(term, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> UpsertManyAsync(IReadOnlyList<ClassificationTerm> terms, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => SaveManyAsync(terms, cancellationToken), cancellationToken);

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

    private async Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> SaveManyAsync(IReadOnlyList<ClassificationTerm> terms, CancellationToken cancellationToken)
    {
        var stored = new List<ClassificationTerm>(terms.Count);
        foreach (var chunk in terms.Chunk(TransactionSize))
        {
            var failure = await SaveChunkAsync(chunk, cancellationToken).MatchAsync(chunkStored => Keep(stored, chunkStored), Option.Some).ConfigureAwait(false);
            if (failure.TryGetValue(out var failed))
                return Result.Failure<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(failed);
        }

        return Result.Success<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(stored);
    }

    private static Option<ClassificationFailure> Keep(List<ClassificationTerm> stored, IReadOnlyList<ClassificationTerm> chunkStored)
    {
        stored.AddRange(chunkStored);

        return Option.None<ClassificationFailure>();
    }

    private async Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> SaveChunkAsync(ClassificationTerm[] chunk, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var categoryIds = chunk.Select(term => term.CategoryId.Value).Distinct().ToList();
        if (await dbContext.Categories.CountAsync(category => categoryIds.Contains(category.Id), cancellationToken).ConfigureAwait(false) != categoryIds.Count)
            return Result.Failure<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(ClassificationFailureFactory.CreateNotFound());

        var texts = chunk.Select(term => term.Text).Distinct().ToList();
        var existing = await dbContext.Terms.AsNoTracking().Where(item => categoryIds.Contains(item.CategoryId) && texts.Contains(item.Text)).ToListAsync(cancellationToken).ConfigureAwait(false);
        var existingByKey = existing.ToDictionary(item => (item.CategoryId, item.Text));
        var resolvedByKey = new Dictionary<(Guid CategoryId, string Text), ClassificationTerm>(chunk.Length);
        var added = new List<TermEntity>();
        var resolved = new ClassificationTerm[chunk.Length];
        for (var index = 0; index < chunk.Length; index++)
        {
            var key = (chunk[index].CategoryId.Value, chunk[index].Text);
            if (!resolvedByKey.TryGetValue(key, out var term))
            {
                term = existingByKey.TryGetValue(key, out var entity) ? TermMapping.ToTerm(entity) : Added(added, chunk[index]);
                resolvedByKey[key] = term;
            }

            resolved[index] = term;
        }

        if (added.Count > 0)
        {
            dbContext.Terms.AddRange(added);
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(resolved);
    }

    private static ClassificationTerm Added(List<TermEntity> added, ClassificationTerm term)
    {
        added.Add(TermMapping.ToEntity(term));

        return term;
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
