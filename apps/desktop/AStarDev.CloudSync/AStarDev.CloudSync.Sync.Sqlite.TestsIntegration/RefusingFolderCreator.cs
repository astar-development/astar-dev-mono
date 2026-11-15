using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class RefusingFolderCreator : IRemoteFolderCreator
{
    public Task<Result<RemoteFolder, ProviderError>> EnsureFolderAsync(AccountId accountId, Option<RemoteItemId> parent, string name, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<RemoteFolder, ProviderError>(ProviderErrorFactory.CreateUnexpected()));
}
