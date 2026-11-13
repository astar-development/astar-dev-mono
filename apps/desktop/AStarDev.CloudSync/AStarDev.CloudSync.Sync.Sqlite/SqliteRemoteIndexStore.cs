using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps an account's remote item index and its change cursor in SQLite (#1398).</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteRemoteIndexStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteRemoteIndexStore> logger) : IRemoteIndexReader, IRemoteIndexWriter, IAccountRemovalParticipant
{
    /// <inheritdoc />
    public Task<Result<Option<ChangeCursor>, ErrorResponse>> GetCursorAsync(AccountId accountId, CancellationToken cancellationToken) => throw Pending(accountId, cancellationToken);

    /// <inheritdoc />
    public Task<Result<Option<ResyncPassId>, ErrorResponse>> GetResyncPassAsync(AccountId accountId, CancellationToken cancellationToken) => throw Pending(accountId, cancellationToken);

    /// <inheritdoc />
    public Task<Result<Option<RemoteItemSnapshot>, ErrorResponse>> GetAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken) => throw Pending(accountId, itemId, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RemoteItemSnapshot>, ErrorResponse>> GetChildrenAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken) => throw Pending(accountId, parent, cancellationToken);

    /// <inheritdoc />
    public Task<Result<Option<LocalRelativePath>, ErrorResponse>> GetPathAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken) => throw Pending(accountId, itemId, cancellationToken);

    /// <inheritdoc />
    public Task<Result<RemoteIndexDelta, ErrorResponse>> ApplyPageAsync(AccountId accountId, RemoteChangePage page, CancellationToken cancellationToken) => throw Pending(accountId, page, cancellationToken);

    /// <inheritdoc />
    public Task<Result<ResyncPassId, ErrorResponse>> BeginResyncAsync(AccountId accountId, CancellationToken cancellationToken) => throw Pending(accountId, cancellationToken);

    /// <inheritdoc />
    public Task<Result<RemoteIndexDelta, ErrorResponse>> CompleteResyncAsync(AccountId accountId, CancellationToken cancellationToken) => throw Pending(accountId, cancellationToken);

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) => throw Pending(accountId, cancellationToken);

    private NotImplementedException Pending(params object?[] arguments) => new($"{dbContextFactory}{logger}{string.Join(',', arguments)}");
}
