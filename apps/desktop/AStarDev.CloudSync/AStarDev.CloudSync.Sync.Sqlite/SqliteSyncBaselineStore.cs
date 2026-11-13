using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps an account's sync baseline in SQLite (#1399). A save runs in one serializable transaction, so concurrent saves for the same path leave one row.</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteSyncBaselineStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteSyncBaselineStore> logger) : ISyncBaselineReader, ISyncBaselineWriter, IAccountRemovalParticipant
{
    private const string Location = nameof(SqliteSyncBaselineStore);
    private const int ChunkSize = 500;
    private const int MaxPageSize = 5000;
    private const char Separator = '/';

    /// <inheritdoc />
    public Task<Result<Option<SyncBaselineEntry>, ErrorResponse>> GetAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadOneAsync(accountId, path, cancellationToken), cancellationToken)
            .MatchAsync(entry => Result.Success<Option<SyncBaselineEntry>, ErrorResponse>(entry), exception => Failed<Option<SyncBaselineEntry>>(exception, "The baseline entry could not be read."));

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<SyncBaselineEntry>, ErrorResponse>> GetUnderAsync(AccountId accountId, RelativeSyncPath under, Option<RelativeSyncPath> after, int limit, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadPageAsync(accountId, under.Value ?? string.Empty, after.Match(path => (string?)path.Value, () => null), Math.Clamp(limit, 1, MaxPageSize), cancellationToken), cancellationToken)
            .MatchAsync(page => Result.Success<IReadOnlyList<SyncBaselineEntry>, ErrorResponse>(page), exception => Failed<IReadOnlyList<SyncBaselineEntry>>(exception, "The baseline could not be read."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> UpsertAsync(AccountId accountId, IReadOnlyList<SyncBaselineEntry> entries, CancellationToken cancellationToken) =>
        Try.RunAsync(() => SaveAsync(accountId, entries, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The baseline could not be saved."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, IReadOnlyList<RelativeSyncPath> paths, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeletePathsAsync(accountId, paths, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The baseline entries could not be removed."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteAccountAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The account's baseline could not be removed."));

    private static SyncBaselineEntry ToEntry(SyncBaselineEntity entity) =>
        SyncBaselineEntryFactory.Create(AccountIdFactory.Create(entity.AccountId), Valid(RelativeSyncPathFactory.Create(entity.Path)), Valid(RemoteItemIdFactory.Create(entity.RemoteItemId)), KindOf(entity));

    private static SyncBaselineKind KindOf(SyncBaselineEntity entity) => entity.IsFolder
        ? SyncBaselineKindFactory.CreateFolder()
        : SyncBaselineKindFactory.CreateFile(entity.Size ?? throw Corrupt(), entity.LastWriteUtc ?? throw Corrupt(), Valid(ContentHashFactory.Create(entity.Hash)), Valid(ContentTagFactory.Create(entity.CTag)), Valid(EntityTagFactory.Create(entity.ETag)));

    private static T Valid<T>(Result<T, ErrorResponse> value) => value.Match(valid => valid, _ => throw Corrupt());

    private static InvalidDataException Corrupt() => new("A stored baseline row is invalid.");

    private static SyncBaselineEntity ToEntity(AccountId accountId, SyncBaselineEntry entry)
    {
        var entity = new SyncBaselineEntity { AccountId = accountId.Value, Path = entry.Path.Value ?? string.Empty, RemoteItemId = entry.ItemId.Value };
        Fill(entity, entry);

        return entity;
    }

    private static void Fill(SyncBaselineEntity entity, SyncBaselineEntry entry)
    {
        entity.RemoteItemId = entry.ItemId.Value;
        if (entry.Kind is SyncBaselineKind.File file)
        {
            entity.IsFolder = false;
            entity.Size = file.Size;
            entity.LastWriteUtc = file.LastWriteUtc;
            entity.Hash = file.Hash.Value;
            entity.CTag = file.CTag.Value;
            entity.ETag = file.ETag.Value;

            return;
        }

        entity.IsFolder = true;
        entity.Size = null;
        entity.LastWriteUtc = null;
        entity.Hash = null;
        entity.CTag = null;
        entity.ETag = null;
    }

    private async Task<Option<SyncBaselineEntry>> ReadOneAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var key = path.Value ?? string.Empty;
        var entity = await dbContext.SyncBaseline.AsNoTracking().Where(item => item.AccountId == accountId.Value && item.Path == key).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return entity is null ? Option.None<SyncBaselineEntry>() : Option.Some(ToEntry(entity));
    }

    private async Task<IReadOnlyList<SyncBaselineEntry>> ReadPageAsync(AccountId accountId, string under, string? after, int limit, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = dbContext.SyncBaseline.AsNoTracking().Where(item => item.AccountId == accountId.Value);
        if (under.Length > 0)
        {
            var prefix = under + Separator;
            query = query.Where(item => item.Path == under || item.Path.StartsWith(prefix));
        }

        if (after is not null)
            query = query.Where(item => item.Path.CompareTo(after) > 0);

        var entities = await query.OrderBy(item => item.Path).Take(limit).ToListAsync(cancellationToken).ConfigureAwait(false);

        return [.. entities.Select(ToEntry)];
    }

    private async Task<Unit> SaveAsync(AccountId accountId, IReadOnlyList<SyncBaselineEntry> entries, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var latest = new Dictionary<string, SyncBaselineEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
            latest[entry.Path.Value ?? string.Empty] = entry;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        foreach (var chunk in latest.Values.Chunk(ChunkSize))
        {
            await SaveChunkAsync(dbContext, accountId, chunk, cancellationToken).ConfigureAwait(false);
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            dbContext.ChangeTracker.Clear();
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private static async Task SaveChunkAsync(SyncDbContext dbContext, AccountId accountId, SyncBaselineEntry[] chunk, CancellationToken cancellationToken)
    {
        var paths = chunk.Select(entry => entry.Path.Value ?? string.Empty).ToList();
        var stored = await dbContext.SyncBaseline.Where(item => item.AccountId == accountId.Value && paths.Contains(item.Path)).ToListAsync(cancellationToken).ConfigureAwait(false);
        var byPath = stored.ToDictionary(item => item.Path, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in chunk)
        {
            if (byPath.TryGetValue(entry.Path.Value ?? string.Empty, out var existing))
                Fill(existing, entry);
            else
                dbContext.SyncBaseline.Add(ToEntity(accountId, entry));
        }
    }

    private async Task<Unit> DeletePathsAsync(AccountId accountId, IReadOnlyList<RelativeSyncPath> paths, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        foreach (var chunk in paths.Select(path => path.Value ?? string.Empty).Chunk(ChunkSize))
        {
            var keys = chunk.ToList();
            _ = await dbContext.SyncBaseline.Where(item => item.AccountId == accountId.Value && keys.Contains(item.Path)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private async Task<Unit> DeleteAccountAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.SyncBaseline.Where(item => item.AccountId == accountId.Value).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Result<T, ErrorResponse> Failed<T>(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<T, ErrorResponse>(new ErrorResponse(message));
    }
}
