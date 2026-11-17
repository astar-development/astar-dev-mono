using System.Data;
using System.Text.Json;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Keeps the links between files and terms in SQLite. Paths match ignoring case through an upper-case key. Every write runs in one serializable transaction, so a failed batch changes nothing and concurrent writes to one file leave one consistent set of links. Removing an account or changing its sync root deletes its links; terms and categories stay. A write for a path that is no longer in the account's sync baseline is dropped inside the write transaction, so a batch streamed before a root change or removal cannot bring old-root links back (#1873). The baseline is read from the <c>SyncBaseline</c> table of the same database file, which the sync persistence owns.</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteFileClassificationStore(IDbContextFactory<ClassificationDbContext> dbContextFactory, ILogger<SqliteFileClassificationStore> logger) : IFileClassificationStore, IAccountRemovalParticipant, ISyncRootChangeParticipant
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

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        RemoveAccountLinksAsync(accountId, cancellationToken).MatchAsync(Result.Success<Unit, ErrorResponse>, _ => Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The account's classification links could not be removed.")));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveRootDataAsync(AccountId accountId, CancellationToken cancellationToken) => RemoveAccountDataAsync(accountId, cancellationToken);

    /// <summary>Deletes every link of the account, automatic and user-assigned. Terms and categories stay. Deleting an account with no links succeeds.</summary>
    /// <param name="accountId">The account whose links are deleted.</param>
    /// <param name="cancellationToken">Cancels the delete.</param>
    /// <returns><see cref="Unit"/> once the links are gone, or <see cref="ClassificationFailure.Unavailable"/> when the database fails.</returns>
    internal Task<Result<Unit, ClassificationFailure>> RemoveAccountLinksAsync(AccountId accountId, CancellationToken cancellationToken) =>
        GuardedStore.RunAsync(logger, Location, () => DeleteAccountAsync(accountId, cancellationToken), cancellationToken);

    private static string Key(RelativeSyncPath path) => (path.Value ?? string.Empty).ToUpperInvariant();

    private static async Task<bool> InBaselineAsync(ClassificationDbContext dbContext, Guid accountId, string key, CancellationToken cancellationToken) =>
        await dbContext.Database.SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM \"SyncBaseline\" WHERE \"AccountId\" = {accountId} AND \"PathKey\" = {key}").SingleAsync(cancellationToken).ConfigureAwait(false) > 0;

    private static FileLinkEntity NewLink(Guid accountId, RelativeSyncPath path, Guid termId, string origin) => new() { AccountId = accountId, Path = path.Value ?? string.Empty, TermId = termId, Origin = origin };

    private async Task<Result<Unit, ClassificationFailure>> ReplaceAsync(IReadOnlyList<FileTermLinks> updates, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        foreach (var accountUpdates in LastUpdatePerFile(updates).GroupBy(update => update.AccountId.Value))
            await ReplaceForAccountAsync(dbContext, accountUpdates.Key, [.. accountUpdates], cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<Unit, ClassificationFailure>(Unit.Instance);
    }

    /// <summary>Keeps the last update for each account and path key, as the per-file writes did: a later update for the same file replaces an earlier one.</summary>
    private static IEnumerable<FileTermLinks> LastUpdatePerFile(IReadOnlyList<FileTermLinks> updates) =>
        updates.GroupBy(update => (update.AccountId.Value, Key(update.Path))).Select(sameFile => sameFile.Last());

    /// <summary>Formats a GUID the way EF Core's SQLite provider stores it (upper-case text), so rows built from JSON match keys written through EF.</summary>
    private static string GuidText(Guid value) => value.ToString("D").ToUpperInvariant();

    /// <summary>Two statements per account, keys and rows passed as one JSON parameter each, so no SQLite parameter limit applies. The delete drops the automatic links of the batch's baseline paths; the insert skips rows that already exist, which after the delete are only user links. Paths not in the baseline are skipped by both.</summary>
    private static async Task ReplaceForAccountAsync(ClassificationDbContext dbContext, Guid accountId, IReadOnlyList<FileTermLinks> updates, CancellationToken cancellationToken)
    {
        var keys = JsonSerializer.Serialize(updates.Select(update => Key(update.Path)).ToArray());
        var rows = JsonSerializer.Serialize(updates.SelectMany(update => update.TermIds.Select(termId => new[] { Key(update.Path), update.Path.Value ?? string.Empty, GuidText(termId.Value) })).ToArray());
        _ = await dbContext.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM "FileLinks"
            WHERE "AccountId" = {accountId} AND "Origin" = {FileLinkOrigins.Auto}
              AND "PathKey" IN (SELECT "value" FROM json_each({keys}))
              AND EXISTS (SELECT 1 FROM "SyncBaseline" WHERE "SyncBaseline"."AccountId" = {accountId} AND "SyncBaseline"."PathKey" = "FileLinks"."PathKey")
            """, cancellationToken).ConfigureAwait(false);
        _ = await dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT OR IGNORE INTO "FileLinks" ("AccountId", "PathKey", "Path", "TermId", "Origin")
            SELECT {accountId}, json_extract("value", '$[0]'), json_extract("value", '$[1]'), json_extract("value", '$[2]'), {FileLinkOrigins.Auto}
            FROM json_each({rows})
            WHERE EXISTS (SELECT 1 FROM "SyncBaseline" WHERE "SyncBaseline"."AccountId" = {accountId} AND "SyncBaseline"."PathKey" = json_extract("value", '$[0]'))
            """, cancellationToken).ConfigureAwait(false);
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
        if (!await InBaselineAsync(dbContext, account, key, cancellationToken).ConfigureAwait(false))
            return Result.Failure<Unit, ClassificationFailure>(ClassificationFailureFactory.CreateFileNotFound());

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

    private async Task<Result<Unit, ClassificationFailure>> DeleteAccountAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var account = accountId.Value;
        _ = await dbContext.FileLinks.Where(link => link.AccountId == account).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<Unit, ClassificationFailure>(Unit.Instance);
    }

    private async Task<Result<IReadOnlyDictionary<TermId, int>, ClassificationFailure>> CountAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var groups = await dbContext.FileLinks.AsNoTracking().GroupBy(link => link.TermId).Select(group => new { TermId = group.Key, Count = group.Count() }).ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyDictionary<TermId, int>, ClassificationFailure>(groups.ToDictionary(group => TermIdFactory.Create(group.TermId), group => group.Count));
    }
}
