using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeRemoteIndexReader : IRemoteIndexReader
{
    public Result<Option<ResyncPassId>, ErrorResponse> Pass { get; set; } = Result.Success<Option<ResyncPassId>, ErrorResponse>(Option.None<ResyncPassId>());

    public CancellationToken SeenToken { get; private set; }

    public AccountId SeenAccount { get; private set; }

    public Task<Result<Option<ResyncPassId>, ErrorResponse>> GetResyncPassAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        SeenAccount = accountId;
        SeenToken = cancellationToken;

        return Task.FromResult(Pass);
    }

    public Task<Result<Option<ChangeCursor>, ErrorResponse>> GetCursorAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Option<RemoteItemSnapshot>, ErrorResponse>> GetAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyList<RemoteItemSnapshot>, ErrorResponse>> GetChildrenAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Option<LocalRelativePath>, ErrorResponse>> GetPathAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<bool, ErrorResponse>> IsCompleteAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotSupportedException();
}
