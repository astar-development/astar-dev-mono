using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class RefusingItemRemover : IRemoteItemRemover
{
    public Task<Result<Unit, ProviderError>> RemoveAsync(AccountId accountId, RemoteItemId itemId, RemovePrecondition precondition, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<Unit, ProviderError>(ProviderErrorFactory.CreateUnexpected()));
}
