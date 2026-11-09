using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public sealed class NoAccountAdders : IProviderCapability<IAccountAdder>
{
    public Result<IAccountAdder, ProviderError> ForProvider(CloudProvider provider) => Result.Failure<IAccountAdder, ProviderError>(ProviderErrorFactory.CreateUnsupported());

    public Task<Result<IAccountAdder, ProviderError>> ForAccountAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(ForProvider(CloudProviderFactory.CreateOneDrive()));
}
