using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync;

/// <summary>Resolves capabilities registered as keyed services keyed by <see cref="CloudProvider"/>.</summary>
/// <typeparam name="TCapability">The capability interface.</typeparam>
/// <param name="services">The container holding the keyed registrations.</param>
/// <param name="accountStore">Maps an account to its provider.</param>
internal sealed class KeyedProviderCapability<TCapability>(IServiceProvider services, IAccountStore accountStore) : IProviderCapability<TCapability>
    where TCapability : class
{
    /// <inheritdoc />
    public Result<TCapability, ProviderError> ForProvider(CloudProvider provider) => throw new NotImplementedException($"{services}{provider}");

    /// <inheritdoc />
    public Task<Result<TCapability, ProviderError>> ForAccountAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountStore}{accountId}{cancellationToken}");
}
