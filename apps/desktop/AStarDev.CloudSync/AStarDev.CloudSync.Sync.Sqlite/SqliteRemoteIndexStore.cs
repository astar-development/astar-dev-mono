using System.Data;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps an account's remote item index and its change cursor in SQLite (#1398).</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="time">Stamps the moment the index first caught up.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteRemoteIndexStore(IDbContextFactory<SyncDbContext> dbContextFactory, TimeProvider time, ILogger<SqliteRemoteIndexStore> logger) : IRemoteIndexReader, IRemoteIndexWriter, IAccountRemovalParticipant
{
    private const string Location = nameof(SqliteRemoteIndexStore);
    private const int LookupChunkSize = 500;
    private const char Separator = '/';

    /// <inheritdoc />
    public Task<Result<Option<ChangeCursor>, ErrorResponse>> GetCursorAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadCursorAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(cursor => Result.Success<Option<ChangeCursor>, ErrorResponse>(cursor), exception => Failed<Option<ChangeCursor>>(exception, "The change cursor could not be read."));

    /// <inheritdoc />
    public Task<Result<bool, ErrorResponse>> IsCompleteAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadCompleteAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(complete => Result.Success<bool, ErrorResponse>(complete), exception => Failed<bool>(exception, "The index state could not be read."));

    /// <inheritdoc />
    public Task<Result<Option<ResyncPassId>, ErrorResponse>> GetResyncPassAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadResyncPassAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(pass => Result.Success<Option<ResyncPassId>, ErrorResponse>(pass), exception => Failed<Option<ResyncPassId>>(exception, "The resync pass could not be read."));

    /// <inheritdoc />
    public Task<Result<Option<RemoteItemSnapshot>, ErrorResponse>> GetAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadItemAsync(accountId, itemId, cancellationToken), cancellationToken)
            .MatchAsync(item => Result.Success<Option<RemoteItemSnapshot>, ErrorResponse>(item), exception => Failed<Option<RemoteItemSnapshot>>(exception, "The remote item could not be read."));

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RemoteItemSnapshot>, ErrorResponse>> GetChildrenAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadChildrenAsync(accountId, parent, cancellationToken), cancellationToken)
            .MatchAsync(children => Result.Success<IReadOnlyList<RemoteItemSnapshot>, ErrorResponse>(children), exception => Failed<IReadOnlyList<RemoteItemSnapshot>>(exception, "The remote folder could not be read."));

    /// <inheritdoc />
    public Task<Result<Option<LocalRelativePath>, ErrorResponse>> GetPathAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ComputePathAsync(accountId, itemId, cancellationToken), cancellationToken)
            .MatchAsync(path => Result.Success<Option<LocalRelativePath>, ErrorResponse>(path), exception => Failed<Option<LocalRelativePath>>(exception, "The remote item path could not be computed."));

    /// <inheritdoc />
    public Task<Result<RemoteIndexDelta, ErrorResponse>> ApplyPageAsync(AccountId accountId, RemoteChangePage page, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ApplyAsync(accountId, page, cancellationToken), cancellationToken)
            .MatchAsync(delta => Result.Success<RemoteIndexDelta, ErrorResponse>(delta), exception => Failed<RemoteIndexDelta>(exception, "The change page could not be applied."));

    /// <inheritdoc />
    public Task<Result<ResyncPassId, ErrorResponse>> BeginResyncAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => BeginAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(pass => Result.Success<ResyncPassId, ErrorResponse>(pass), exception => Failed<ResyncPassId>(exception, "The resync could not be started."));

    /// <inheritdoc />
    public Task<Result<RemoteIndexDelta, ErrorResponse>> CompleteResyncAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => CompleteAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(delta => Result.Success<RemoteIndexDelta, ErrorResponse>(delta), exception => Failed<RemoteIndexDelta>(exception, "The resync could not be completed."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The account's remote index could not be removed."));

    private static RemoteItemSnapshot[] ToSnapshot(RemoteItemEntity entity) =>
        RemoteItemIdFactory.Create(entity.RemoteItemId).Match(
            id => EntityTagFactory.Create(entity.ETag).Match(eTag => new[] { RemoteItemSnapshotFactory.Create(id, ParentOf(entity), entity.Name, KindOf(entity), entity.LastModified, eTag) }, _ => Array.Empty<RemoteItemSnapshot>()),
            _ => Array.Empty<RemoteItemSnapshot>());

    private static Option<RemoteItemId> ParentOf(RemoteItemEntity entity) =>
        RemoteItemIdFactory.Create(entity.ParentId).Match(parent => Option.Some(parent), _ => Option.None<RemoteItemId>());

    private static RemoteItemKind KindOf(RemoteItemEntity entity)
    {
        if (entity.IsDeleted)
            return RemoteItemKindFactory.CreateDeleted();

        return entity.IsFolder
            ? RemoteItemKindFactory.CreateFolder()
            : RemoteItemKindFactory.CreateFile(entity.Size ?? 0, ContentHashFactory.Create(entity.Hash).Match(hash => Option.Some(hash), _ => Option.None<ContentHash>()), ContentTagFactory.Create(entity.CTag ?? entity.ETag).Match(tag => tag, _ => default));
    }

    private static List<RemoteItemSnapshot> LastOccurrenceWins(IEnumerable<RemoteItemSnapshot> items) =>
        [.. items.GroupBy(item => item.Id.Value, StringComparer.Ordinal).Select(group => group.Last())];

    private static RemoteItemEntity NewEntity(AccountId accountId, RemoteItemSnapshot snapshot, Guid? pass)
    {
        var entity = new RemoteItemEntity { AccountId = accountId.Value, RemoteItemId = snapshot.Id.Value };
        Fill(entity, snapshot, pass);

        return entity;
    }

    private static void Fill(RemoteItemEntity entity, RemoteItemSnapshot snapshot, Guid? pass)
    {
        entity.ParentId = snapshot.ParentId.Match(parent => (string?)parent.Value, () => null);
        entity.Name = snapshot.Name;
        entity.ETag = snapshot.ETag.Value;
        entity.LastModified = snapshot.LastModified;
        entity.IsDeleted = false;
        entity.LastSeenPass = pass;
        entity.IsFolder = snapshot.Kind is not RemoteItemKind.File;
        entity.Size = snapshot.Kind is RemoteItemKind.File file ? file.Size : null;
        entity.Hash = snapshot.Kind is RemoteItemKind.File withHash ? HashOf(withHash) : null;
        entity.CTag = snapshot.Kind is RemoteItemKind.File withTag ? withTag.CTag.Value : null;
    }

    private static string? HashOf(RemoteItemKind.File file) => file.Hash.Match(hash => (string?)hash.Value, () => null);

    private static bool ContentChanged(RemoteItemEntity stored, RemoteItemSnapshot snapshot) => snapshot.Kind switch
    {
        RemoteItemKind.File file => stored.IsFolder || !string.Equals(stored.CTag, file.CTag.Value, StringComparison.Ordinal) || !string.Equals(stored.Hash, HashOf(file), StringComparison.Ordinal),
        _ => !stored.IsFolder,
    };

    private static bool PlaceChanged(RemoteItemEntity stored, RemoteItemSnapshot snapshot) =>
        !string.Equals(stored.ParentId, snapshot.ParentId.Match(parent => (string?)parent.Value, () => null), StringComparison.Ordinal) || !string.Equals(stored.Name, snapshot.Name, StringComparison.Ordinal);

    private async Task<Option<ChangeCursor>> ReadCursorAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var cursor = await dbContext.RemoteCursors.AsNoTracking().Where(row => row.AccountId == accountId.Value).Select(row => row.Cursor).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return ChangeCursorFactory.Create(cursor).Match(found => Option.Some(found), _ => Option.None<ChangeCursor>());
    }

    private async Task<bool> ReadCompleteAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await dbContext.RemoteCursors.AsNoTracking().AnyAsync(row => row.AccountId == accountId.Value && row.CaughtUpAt != null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Option<ResyncPassId>> ReadResyncPassAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var pass = await dbContext.RemoteCursors.AsNoTracking().Where(row => row.AccountId == accountId.Value).Select(row => row.ResyncPass).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return pass is { } value ? Option.Some(new ResyncPassId(value)) : Option.None<ResyncPassId>();
    }

    private async Task<Option<RemoteItemSnapshot>> ReadItemAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var entity = await dbContext.RemoteItems.AsNoTracking().Where(item => item.AccountId == accountId.Value && item.RemoteItemId == itemId.Value).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return entity is null ? Option.None<RemoteItemSnapshot>() : ToSnapshot(entity).FirstOrNone();
    }

    private async Task<IReadOnlyList<RemoteItemSnapshot>> ReadChildrenAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var live = dbContext.RemoteItems.AsNoTracking().Where(item => item.AccountId == accountId.Value && !item.IsDeleted);
        var children = await parent.Match(
            id => live.Where(item => item.ParentId == id.Value),
            () => live.Where(item => item.ParentId == null || !dbContext.RemoteItems.Any(other => other.AccountId == accountId.Value && other.RemoteItemId == item.ParentId && !other.IsDeleted)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return [.. children.SelectMany(ToSnapshot).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)];
    }

    private async Task<Option<LocalRelativePath>> ComputePathAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var names = new List<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = itemId.Value;

        while (current is not null)
        {
            if (!visited.Add(current))
                return Option.None<LocalRelativePath>();

            var key = current;
            var step = await dbContext.RemoteItems.AsNoTracking().Where(item => item.AccountId == accountId.Value && item.RemoteItemId == key).Select(item => new { item.Name, item.ParentId }).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (step is null)
                break;

            names.Add(step.Name);
            current = step.ParentId;
        }

        return names.Count == 0
            ? Option.None<LocalRelativePath>()
            : LocalRelativePathFactory.Create(string.Join(Separator, Enumerable.Reverse(names))).Match(path => Option.Some(path), _ => Option.None<LocalRelativePath>());
    }

    private async Task<RemoteIndexDelta> ApplyAsync(AccountId accountId, RemoteChangePage page, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var cursorRow = await dbContext.RemoteCursors.Where(row => row.AccountId == accountId.Value).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var pass = cursorRow?.ResyncPass;
        var wanted = LastOccurrenceWins(page.Items);
        var stored = await LoadAsync(dbContext, accountId, wanted.Select(item => item.Id.Value), cancellationToken).ConfigureAwait(false);
        var changes = new List<RemoteChange>();

        foreach (var snapshot in wanted.Where(item => item.Kind is not RemoteItemKind.Deleted))
            changes.AddRange(Upsert(dbContext, accountId, snapshot, stored, pass));

        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var snapshot in wanted.Where(item => item.Kind is RemoteItemKind.Deleted))
            changes.AddRange(await DeleteTreeAsync(dbContext, accountId, snapshot.Id.Value, stored, pass, cancellationToken).ConfigureAwait(false));

        SaveCursor(dbContext, accountId, cursorRow, page.Next, time.GetUtcNow());
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return RemoteIndexDeltaFactory.Create(changes);
    }

    internal static async Task<Dictionary<string, RemoteItemEntity>> LoadAsync(SyncDbContext dbContext, AccountId accountId, IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        var stored = new Dictionary<string, RemoteItemEntity>(StringComparer.Ordinal);

        foreach (var chunk in ids.Chunk(LookupChunkSize))
        {
            var found = await dbContext.RemoteItems.Where(item => item.AccountId == accountId.Value && chunk.Contains(item.RemoteItemId)).ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var entity in found)
                stored[entity.RemoteItemId] = entity;
        }

        return stored;
    }

    internal static RemoteChange[] Upsert(SyncDbContext dbContext, AccountId accountId, RemoteItemSnapshot snapshot, Dictionary<string, RemoteItemEntity> stored, Guid? pass)
    {
        if (!stored.TryGetValue(snapshot.Id.Value, out var entity))
        {
            var created = NewEntity(accountId, snapshot, pass);
            _ = dbContext.RemoteItems.Add(created);
            stored[created.RemoteItemId] = created;

            return [RemoteChangeFactory.CreateAdded(snapshot)];
        }

        var wasDeleted = entity.IsDeleted;
        var oldParent = ParentOf(entity);
        var oldName = entity.Name;
        var moved = PlaceChanged(entity, snapshot);
        var modified = ContentChanged(entity, snapshot);
        Fill(entity, snapshot, pass);

        if (wasDeleted)
            return [RemoteChangeFactory.CreateAdded(snapshot)];

        if (moved)
            return [RemoteChangeFactory.CreateMoved(snapshot, oldParent, oldName)];

        return modified ? [RemoteChangeFactory.CreateModified(snapshot)] : [];
    }

    private static async Task<List<RemoteChange>> DeleteTreeAsync(SyncDbContext dbContext, AccountId accountId, string rootId, Dictionary<string, RemoteItemEntity> stored, Guid? pass, CancellationToken cancellationToken)
    {
        var changes = new List<RemoteChange>();

        if (!stored.TryGetValue(rootId, out var root) || root.IsDeleted)
            return changes;

        var seen = new HashSet<string>(StringComparer.Ordinal) { rootId };
        var level = new List<string> { rootId };
        MarkDeleted(root, pass, changes);

        while (root.IsFolder && level.Count > 0)
        {
            var next = new List<string>();

            foreach (var chunk in level.Chunk(LookupChunkSize))
            {
                var children = await dbContext.RemoteItems.Where(item => item.AccountId == accountId.Value && item.ParentId != null && chunk.Contains(item.ParentId) && !item.IsDeleted).ToListAsync(cancellationToken).ConfigureAwait(false);

                foreach (var child in children.Where(child => seen.Add(child.RemoteItemId)))
                {
                    MarkDeleted(child, pass, changes);

                    if (child.IsFolder)
                        next.Add(child.RemoteItemId);
                }
            }

            level = next;
        }

        return changes;
    }

    private static void MarkDeleted(RemoteItemEntity entity, Guid? pass, List<RemoteChange> changes)
    {
        entity.IsDeleted = true;
        entity.LastSeenPass = pass;
        changes.AddRange(RemoteItemIdFactory.Create(entity.RemoteItemId).Match(id => new[] { RemoteChangeFactory.CreateDeleted(id) }, _ => Array.Empty<RemoteChange>()));
    }

    private static void SaveCursor(SyncDbContext dbContext, AccountId accountId, RemoteCursorEntity? row, PageContinuation next, DateTimeOffset now)
    {
        var cursor = next switch
        {
            PageContinuation.MorePages more => more.Next.Value,
            PageContinuation.CaughtUp caughtUp => caughtUp.Delta.Value,
            _ => null,
        };

        if (cursor is null)
            return;

        var caughtUpAt = next is PageContinuation.CaughtUp ? now : row?.CaughtUpAt;
        if (row is null)
            _ = dbContext.RemoteCursors.Add(new RemoteCursorEntity { AccountId = accountId.Value, Cursor = cursor, CaughtUpAt = caughtUpAt });
        else
        {
            row.Cursor = cursor;
            row.CaughtUpAt = caughtUpAt;
        }
    }

    private async Task<ResyncPassId> BeginAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var row = await dbContext.RemoteCursors.Where(cursor => cursor.AccountId == accountId.Value).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (row?.ResyncPass is { } running)
            return new ResyncPassId(running);

        var pass = Guid.NewGuid();

        if (row is null)
            _ = dbContext.RemoteCursors.Add(new RemoteCursorEntity { AccountId = accountId.Value, ResyncPass = pass });
        else
            row.ResyncPass = pass;

        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return new ResyncPassId(pass);
    }

    private async Task<RemoteIndexDelta> CompleteAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var row = await dbContext.RemoteCursors.Where(cursor => cursor.AccountId == accountId.Value).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (row?.ResyncPass is not { } pass)
            return RemoteIndexDeltaFactory.Create([]);

        var unseen = dbContext.RemoteItems.Where(item => item.AccountId == accountId.Value && !item.IsDeleted && item.LastSeenPass != pass);
        var missingIds = await unseen.Select(item => item.RemoteItemId).ToListAsync(cancellationToken).ConfigureAwait(false);
        _ = await unseen.ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsDeleted, true), cancellationToken).ConfigureAwait(false);
        row.ResyncPass = null;
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return RemoteIndexDeltaFactory.Create(missingIds.SelectMany(id => RemoteItemIdFactory.Create(id).Match(found => new[] { RemoteChangeFactory.CreateDeleted(found) }, _ => Array.Empty<RemoteChange>())));
    }

    private async Task<Unit> DeleteAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        _ = await dbContext.RemoteItems.Where(item => item.AccountId == accountId.Value).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.RemoteCursors.Where(cursor => cursor.AccountId == accountId.Value).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Result<T, ErrorResponse> Failed<T>(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<T, ErrorResponse>(new ErrorResponse(message));
    }
}
