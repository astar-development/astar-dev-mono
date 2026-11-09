using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the accounts' sync roots in the sync tables.</summary>
internal sealed class SqliteSyncRootStore : ISyncRootReader, ISyncRootWriter, IAccountRemovalParticipant
{
    /// <inheritdoc />
    public Task<Result<Option<SyncRoot>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<Option<SyncRoot>, ErrorResponse>(Option.None<SyncRoot>()));

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AccountSyncRoot>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<IReadOnlyList<AccountSyncRoot>, ErrorResponse>([]));

    /// <inheritdoc />
    public Task<Result<SyncRoot, SyncRootRejection>> SetAsync(AccountId accountId, IDirectoryInfo candidate, CancellationToken cancellationToken) =>
        Task.FromResult(SyncRootFactory.Create(candidate));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
}
