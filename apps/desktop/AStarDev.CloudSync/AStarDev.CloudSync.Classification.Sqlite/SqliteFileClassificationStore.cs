using System.Data;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Keeps the links between files and terms in SQLite. Paths match ignoring case through an upper-case key. Every write runs in one serializable transaction, so a failed batch changes nothing and concurrent writes to one file leave one consistent set of links.</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteFileClassificationStore(IDbContextFactory<ClassificationDbContext> dbContextFactory, ILogger<SqliteFileClassificationStore> logger) : IFileClassificationStore
{
    private const string Location = nameof(SqliteFileClassificationStore);

    /// <inheritdoc />
    public Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(AccountId accountId, RelativeSyncPath path, IReadOnlyList<TermId> termIds, CancellationToken cancellationToken) =>
        ReplaceAutoLinksAsync([FileTermLinksFactory.Create(accountId, path, termIds)], cancellationToken);

    /// <inheritdoc />
    public Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(IReadOnlyList<FileTermLinks> updates, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => ReplaceAsync(updates, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<Unit, ClassificationFailure>> AssignUserLinkAsync(AccountId accountId, RelativeSyncPath path, TermId termId, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => AssignAsync(accountId, path, termId, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<TermId>, ClassificationFailure>> GetTermsForFileAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => ReadTermsAsync(accountId, path, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ClassifiableFile>, ClassificationFailure>> GetFilesForTermAsync(TermId termId, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => ReadFilesAsync(termId, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<Unit, ClassificationFailure>> RemoveAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => DeleteAsync(accountId, path, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyDictionary<TermId, int>, ClassificationFailure>> GetFileCountsAsync(CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => CountAsync(cancellationToken), cancellationToken);

    private static string Key(RelativeSyncPath path) => (path.Value ?? string.Empty).ToUpperInvariant();

    private static FileLinkEntity NewLink(Guid accountId, RelativeSyncPath path, Guid termId, string origin) => new() { AccountId = accountId, Path = path.Value ?? string.Empty, TermId = termId, Origin = origin };

    private async Task<Result<Unit, ClassificationFailure>> ReplaceAsync(IReadOnlyList<FileTermLinks> updates, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        foreach (var update in updates)
        {
            var account = update.AccountId.Value;
            var key = Key(update.Path);
            _ = await dbContext.FileLinks.Where(link => link.AccountId == account && link.PathKey == key && link.Origin == FileLinkOrigins.Auto).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            var kept = await dbContext.FileLinks.Where(link => link.AccountId == account && link.PathKey == key).Select(link => link.TermId).ToListAsync(cancellationToken).ConfigureAwait(false);
            dbContext.FileLinks.AddRange(update.TermIds.Select(termId => termId.Value).Distinct().Where(termId => !kept.Contains(termId)).Select(termId => NewLink(account, update.Path, termId, FileLinkOrigins.Auto)));
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            dbContext.ChangeTracker.Clear();
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<Unit, ClassificationFailure>(Unit.Instance);
    }

    private async Task<Result<Unit, ClassificationFailure>> AssignAsync(AccountId accountId, RelativeSyncPath path, TermId termId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var term = termId.Value;
        if (!await dbContext.Terms.AnyAsync(item => item.Id == term, cancellationToken).ConfigureAwait(false))
            return Result.Failure<Unit, ClassificationFailure>(ClassificationFailureFactory.CreateNotFound());

        var account = accountId.Value;
        var key = Key(path);
        var link = await dbContext.FileLinks.FirstOrDefaultAsync(item => item.AccountId == account && item.PathKey == key && item.TermId == term, cancellationToken).ConfigureAwait(false);
        if (link is null)
            _ = dbContext.FileLinks.Add(NewLink(account, path, term, FileLinkOrigins.User));
        else
            link.Origin = FileLinkOrigins.User;

        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<Unit, ClassificationFailure>(Unit.Instance);
    }

    private async Task<Result<IReadOnlyList<TermId>, ClassificationFailure>> ReadTermsAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var account = accountId.Value;
        var key = Key(path);
        var termIds = await dbContext.FileLinks.AsNoTracking().Where(link => link.AccountId == account && link.PathKey == key).Select(link => link.TermId).ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<TermId>, ClassificationFailure>([.. termIds.Select(TermIdFactory.Create)]);
    }

    private async Task<Result<IReadOnlyList<ClassifiableFile>, ClassificationFailure>> ReadFilesAsync(TermId termId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var term = termId.Value;
        var links = await dbContext.FileLinks.AsNoTracking().Where(link => link.TermId == term).OrderBy(link => link.AccountId).ThenBy(link => link.PathKey).ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<ClassifiableFile>, ClassificationFailure>([.. links.Select(ToFile)]);
    }

    private static ClassifiableFile ToFile(FileLinkEntity link) =>
        ClassifiableFileFactory.Create(AccountIdFactory.Create(link.AccountId), RelativeSyncPathFactory.Create(link.Path).Match(path => path, _ => throw TermMapping.Corrupt()));

    private async Task<Result<Unit, ClassificationFailure>> DeleteAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var account = accountId.Value;
        var key = Key(path);
        _ = await dbContext.FileLinks.Where(link => link.AccountId == account && link.PathKey == key).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<Unit, ClassificationFailure>(Unit.Instance);
    }

    private async Task<Result<IReadOnlyDictionary<TermId, int>, ClassificationFailure>> CountAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var groups = await dbContext.FileLinks.AsNoTracking().GroupBy(link => link.TermId).Select(group => new { TermId = group.Key, Count = group.Count() }).ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyDictionary<TermId, int>, ClassificationFailure>(groups.ToDictionary(group => TermIdFactory.Create(group.TermId), group => group.Count));
    }
}
