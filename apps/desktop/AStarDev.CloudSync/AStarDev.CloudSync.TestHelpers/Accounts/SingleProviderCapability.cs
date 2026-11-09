using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class SingleProviderCapability<TCapability>(TCapability capability) : IProviderCapability<TCapability>
    where TCapability : class
{
    public Result<TCapability, ProviderError> ForProvider(CloudProvider provider) => Result.Success<TCapability, ProviderError>(capability);

    public Task<Result<TCapability, ProviderError>> ForAccountAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(ForProvider(CloudProviderFactory.CreateOneDrive()));
}
