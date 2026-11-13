using System.Data;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the attention queue in SQLite (#1405). A write is one serializable transaction, so concurrent passes run one after the other and a crash leaves no partial rows.</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="logger">Reports writes that fail, and items recorded or closed, by kind and path hash only.</param>
internal sealed class SqliteSyncAttentionStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteSyncAttentionStore> logger) : ISyncAttentionReader, ISyncAttentionWriter, ISyncAttentionCloser, IAccountRemovalParticipant
{
    private const string Location = nameof(SqliteSyncAttentionStore);
    private const string EmptyPathError = "An attention item needs a path.";
    private const string ConflictKeyPrefix = "c:";
    private const string HeldKeyPrefix = "h:";

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<SyncAttentionItem>, ErrorResponse>> GetOpenAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadOpenAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(items => Result.Success<IReadOnlyList<SyncAttentionItem>, ErrorResponse>(items), exception => Failed<IReadOnlyList<SyncAttentionItem>>(exception, "The attention queue could not be read."));

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RelativeSyncPath>, ErrorResponse>> GetConflictedPathsAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadConflictedPathsAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(paths => Result.Success<IReadOnlyList<RelativeSyncPath>, ErrorResponse>(paths), exception => Failed<IReadOnlyList<RelativeSyncPath>>(exception, "The conflicted paths could not be read."));

    /// <inheritdoc />
    public Task<Result<AttentionCounts, ErrorResponse>> CountAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => CountOpenAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(counts => Result.Success<AttentionCounts, ErrorResponse>(counts), exception => Failed<AttentionCounts>(exception, "The attention queue could not be counted."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RecordAsync(IReadOnlyList<SyncAttentionItem> items, CancellationToken cancellationToken) =>
        HasEmptyPath(items)
            ? Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse(EmptyPathError)))
            : Try.RunAsync(() => WriteAsync(items, cancellationToken), cancellationToken)
                .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The attention items could not be saved."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> CloseAsync(ConflictId id, AttentionResolution resolution, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteByIdAsync(id.Value, "conflict", resolution, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The conflict could not be closed."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> CloseAsync(HeldDeletionId id, AttentionResolution resolution, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteByIdAsync(id.Value, "held deletions", resolution, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The held deletions could not be closed."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteAccountAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The account's attention items could not be removed."));

    private static bool HasEmptyPath(IReadOnlyList<SyncAttentionItem> items) => items.OfType<SyncAttentionItem.Conflict>().Any(conflict => string.IsNullOrEmpty(conflict.Path.Value));

    private static string KeyOf(SyncAttentionItem item) => item switch
    {
        SyncAttentionItem.Conflict conflict => ConflictKeyPrefix + (conflict.Path.Value ?? string.Empty).ToUpperInvariant(),
        SyncAttentionItem.HeldDeletions held => $"{HeldKeyPrefix}{held.PairId.Value:N}:{NameOf(held.Direction)}",
        _ => throw new InvalidOperationException("Unknown attention item."),
    };

    private static string NameOf(DeletionDirection direction) => direction is DeletionDirection.DeleteRemote ? nameof(DeletionDirection.DeleteRemote) : nameof(DeletionDirection.DeleteLocal);

    private static string NameOf(ConflictKind kind) => kind.GetType().Name;

    private static DeletionDirection DirectionOf(string? name) => name == nameof(DeletionDirection.DeleteRemote) ? DeletionDirectionFactory.CreateDeleteRemote() : DeletionDirectionFactory.CreateDeleteLocal();

    private static ConflictKind KindOf(string? name) => name switch
    {
        nameof(ConflictKind.BothModified) => ConflictKindFactory.CreateBothModified(),
        nameof(ConflictKind.LocalModifiedRemoteDeleted) => ConflictKindFactory.CreateLocalModifiedRemoteDeleted(),
        nameof(ConflictKind.LocalDeletedRemoteModified) => ConflictKindFactory.CreateLocalDeletedRemoteModified(),
        nameof(ConflictKind.BothAdded) => ConflictKindFactory.CreateBothAdded(),
        nameof(ConflictKind.MoveTargetTaken) => ConflictKindFactory.CreateMoveTargetTaken(),
        nameof(ConflictKind.RemoteChangedDuringTransfer) => ConflictKindFactory.CreateRemoteChangedDuringTransfer(),
        _ => throw new InvalidOperationException("A stored conflict has an unknown kind."),
    };

    private static RelativeSyncPath PathOf(string? text) => RelativeSyncPathFactory.Create(text).Match(path => path, _ => throw new InvalidOperationException("A stored attention path is invalid."));

    private static Option<T> OptionOf<T>(string? text, Func<string, T> create) => text is null ? Option.None<T>() : Option.Some(create(text));

    private static SideSnapshot LocalSideOf(SyncAttentionEntity entity) =>
        entity.LocalPresent ? SideSnapshotFactory.CreatePresent(entity.LocalSize, entity.LocalModified ?? default, OptionOf(entity.LocalHash, text => new ContentHash(text)), OptionOf(entity.LocalTag, text => new ContentTag(text))) : SideSnapshotFactory.CreateAbsent();

    private static SideSnapshot RemoteSideOf(SyncAttentionEntity entity) =>
        entity.RemotePresent ? SideSnapshotFactory.CreatePresent(entity.RemoteSize, entity.RemoteModified ?? default, OptionOf(entity.RemoteHash, text => new ContentHash(text)), OptionOf(entity.RemoteTag, text => new ContentTag(text))) : SideSnapshotFactory.CreateAbsent();

    private static SyncAttentionItem ToItem(SyncAttentionEntity entity) => entity.IsConflict
        ? SyncAttentionItemFactory.CreateConflict(new ConflictId(entity.Id), AccountIdFactory.Create(entity.AccountId), PathOf(entity.Path), KindOf(entity.ConflictKind), LocalSideOf(entity), RemoteSideOf(entity), entity.DetectedAt)
        : SyncAttentionItemFactory.CreateHeldDeletions(new HeldDeletionId(entity.Id), AccountIdFactory.Create(entity.AccountId), SyncPairIdFactory.Create(entity.PairId ?? Guid.Empty), DirectionOf(entity.Direction), entity.HeldPaths.Select(held => PathOf(held.Path)), entity.DetectedAt);

    private static void CopySide(SideSnapshot side, Action<bool, long, DateTimeOffset?, string?, string?> assign)
    {
        if (side is SideSnapshot.Present present)
            assign(true, present.Size, present.LastModified, present.Hash.Match(hash => (string?)hash.Value, () => null), present.Tag.Match(tag => (string?)tag.Value, () => null));
        else
            assign(false, 0, null, null, null);
    }

    private static void Fill(SyncAttentionEntity entity, SyncAttentionItem item)
    {
        if (item is SyncAttentionItem.Conflict conflict)
        {
            entity.IsConflict = true;
            entity.Path = conflict.Path.Value;
            entity.ConflictKind = NameOf(conflict.Kind);
            CopySide(conflict.Local, (present, size, modified, hash, tag) => (entity.LocalPresent, entity.LocalSize, entity.LocalModified, entity.LocalHash, entity.LocalTag) = (present, size, modified, hash, tag));
            CopySide(conflict.Remote, (present, size, modified, hash, tag) => (entity.RemotePresent, entity.RemoteSize, entity.RemoteModified, entity.RemoteHash, entity.RemoteTag) = (present, size, modified, hash, tag));
        }
        else if (item is SyncAttentionItem.HeldDeletions held)
        {
            entity.IsConflict = false;
            entity.PairId = held.PairId.Value;
            entity.Direction = NameOf(held.Direction);
        }
    }

    private static SyncAttentionEntity NewEntity(SyncAttentionItem item, string key)
    {
        var entity = item switch
        {
            SyncAttentionItem.Conflict conflict => new SyncAttentionEntity { Id = conflict.Id.Value, AccountId = conflict.AccountId.Value, DetectedAt = conflict.DetectedAt },
            SyncAttentionItem.HeldDeletions held => new SyncAttentionEntity { Id = held.Id.Value, AccountId = held.AccountId.Value, DetectedAt = held.DetectedAt },
            _ => throw new InvalidOperationException("Unknown attention item."),
        };
        entity.ItemKey = key;
        Fill(entity, item);

        return entity;
    }

    private static Guid AccountOf(SyncAttentionItem item) => item switch
    {
        SyncAttentionItem.Conflict conflict => conflict.AccountId.Value,
        SyncAttentionItem.HeldDeletions held => held.AccountId.Value,
        _ => throw new InvalidOperationException("Unknown attention item."),
    };

    private static IEnumerable<SyncAttentionPathEntity> HeldPathsOf(SyncAttentionItem item, Guid attentionId) =>
        item is SyncAttentionItem.HeldDeletions held ? held.Paths.Select(path => new SyncAttentionPathEntity { AttentionId = attentionId, Path = path.Value ?? string.Empty }) : [];

    private async Task<IReadOnlyList<SyncAttentionItem>> ReadOpenAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var entities = await dbContext.SyncAttention.AsNoTracking().Include(item => item.HeldPaths).Where(item => item.AccountId == accountId.Value).ToListAsync(cancellationToken).ConfigureAwait(false);

        return [.. entities.OrderBy(entity => entity.DetectedAt).ThenBy(entity => entity.Id).Select(ToItem)];
    }

    private async Task<IReadOnlyList<RelativeSyncPath>> ReadConflictedPathsAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var paths = await dbContext.SyncAttention.AsNoTracking().Where(item => item.AccountId == accountId.Value && item.IsConflict).Select(item => item.Path).ToListAsync(cancellationToken).ConfigureAwait(false);

        return [.. paths.Select(PathOf).OrderBy(path => path.Value, StringComparer.OrdinalIgnoreCase)];
    }

    private async Task<AttentionCounts> CountOpenAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var mine = dbContext.SyncAttention.AsNoTracking().Where(item => item.AccountId == accountId.Value);
        var conflicts = await mine.CountAsync(item => item.IsConflict, cancellationToken).ConfigureAwait(false);
        var sets = await mine.CountAsync(item => !item.IsConflict, cancellationToken).ConfigureAwait(false);
        var paths = await mine.Where(item => !item.IsConflict).SelectMany(item => item.HeldPaths).CountAsync(cancellationToken).ConfigureAwait(false);

        return AttentionCountsFactory.Create(conflicts, sets, paths);
    }

    private async Task<Unit> WriteAsync(IReadOnlyList<SyncAttentionItem> items, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        foreach (var item in items)
            await UpsertAsync(dbContext, item, cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        foreach (var item in items)
            LogRecorded(item);

        return Unit.Instance;
    }

    private static async Task UpsertAsync(SyncDbContext dbContext, SyncAttentionItem item, CancellationToken cancellationToken)
    {
        var key = KeyOf(item);
        var accountId = AccountOf(item);
        var existing = await dbContext.SyncAttention.Include(entity => entity.HeldPaths).FirstOrDefaultAsync(entity => entity.AccountId == accountId && entity.ItemKey == key, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            var created = NewEntity(item, key);
            created.HeldPaths.AddRange(HeldPathsOf(item, created.Id));
            _ = dbContext.SyncAttention.Add(created);
        }
        else
        {
            Fill(existing, item);
            existing.HeldPaths.Clear();
            existing.HeldPaths.AddRange(HeldPathsOf(item, existing.Id));
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private void LogRecorded(SyncAttentionItem item)
    {
        if (item is SyncAttentionItem.Conflict conflict)
        {
            using var scope = SyncLogScope.Begin(logger, conflict.AccountId);
            var message = $"Recorded a {NameOf(conflict.Kind)} conflict at path {LogHash.Of(conflict.Path.Value ?? string.Empty)}.";
            LogMessage.Information(logger, Location, message);
        }
        else if (item is SyncAttentionItem.HeldDeletions held)
        {
            using var scope = SyncLogScope.Begin(logger, held.AccountId);
            var message = $"Recorded {held.Paths.Count} held deletions, {NameOf(held.Direction)}.";
            LogMessage.Information(logger, Location, message);
        }
    }

    private async Task<Unit> DeleteByIdAsync(Guid id, string what, AttentionResolution resolution, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var removed = await dbContext.SyncAttention.Where(item => item.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        var message = $"Closed {removed} {what} item as {resolution.GetType().Name}.";
        LogMessage.Information(logger, Location, message);

        return Unit.Instance;
    }

    private async Task<Unit> DeleteAccountAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.SyncAttention.Where(item => item.AccountId == accountId.Value).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Result<T, ErrorResponse> Failed<T>(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<T, ErrorResponse>(new ErrorResponse(message));
    }
}
