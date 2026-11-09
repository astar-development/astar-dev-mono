using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Finds a provider's implementation of a capability. Providers register each capability as a keyed service keyed by <see cref="CloudProvider"/>.</summary>
/// <typeparam name="TCapability">The capability interface, for example <see cref="IRemoteFolderBrowser"/>.</typeparam>
public interface IProviderCapability<TCapability>
    where TCapability : class
{
    /// <summary>The capability for a provider, for callers that have no account yet (adding an account).</summary>
    /// <param name="provider">The provider.</param>
    /// <returns>The implementation, or <see cref="ProviderError.Unsupported"/> when the provider registered none.</returns>
    Result<TCapability, ProviderError> ForProvider(CloudProvider provider);

    /// <summary>The capability for a stored account's provider.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the account lookup.</param>
    /// <returns>The implementation; <see cref="ProviderError.NotFound"/> for an unknown account, <see cref="ProviderError.Transient"/> when the accounts cannot be read, or <see cref="ProviderError.Unsupported"/>.</returns>
    Task<Result<TCapability, ProviderError>> ForAccountAsync(AccountId accountId, CancellationToken cancellationToken);
}
