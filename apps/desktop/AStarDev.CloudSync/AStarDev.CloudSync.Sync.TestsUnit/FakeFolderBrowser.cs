using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeFolderBrowser : IRemoteFolderBrowser
{
    public Task<Result<IReadOnlyList<RemoteFolder>, ProviderError>> GetChildFoldersAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken) => Task.FromResult(Result.Success<IReadOnlyList<RemoteFolder>, ProviderError>([]));
}
