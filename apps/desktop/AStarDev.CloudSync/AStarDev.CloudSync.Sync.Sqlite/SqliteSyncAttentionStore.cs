using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the attention queue in SQLite (#1405).</summary>
/// <param name="dbContextFactory">Creates a context per operation.</param>
/// <param name="logger">Reports writes that fail, and items recorded or closed, without paths.</param>
internal sealed class SqliteSyncAttentionStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteSyncAttentionStore> logger) : ISyncAttentionReader, ISyncAttentionWriter, ISyncAttentionCloser, IAccountRemovalParticipant
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<SyncAttentionItem>, ErrorResponse>> GetOpenAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{dbContextFactory.GetType().Name} {logger.GetType().Name} {accountId} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RelativeSyncPath>, ErrorResponse>> GetConflictedPathsAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<AttentionCounts, ErrorResponse>> CountAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RecordAsync(IReadOnlyList<SyncAttentionItem> items, CancellationToken cancellationToken) => throw new NotImplementedException($"{items} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> CloseAsync(ConflictId id, AttentionResolution resolution, CancellationToken cancellationToken) => throw new NotImplementedException($"{id} {resolution} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> CloseAsync(HeldDeletionId id, AttentionResolution resolution, CancellationToken cancellationToken) => throw new NotImplementedException($"{id} {resolution} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId} {cancellationToken}");
}
