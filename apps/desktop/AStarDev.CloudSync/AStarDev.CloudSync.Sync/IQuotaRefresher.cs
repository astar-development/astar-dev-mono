using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads an account's current storage usage from its provider. Providers register it as a keyed singleton keyed by <see cref="CloudProvider"/> (#1565).</summary>
public interface IQuotaRefresher
{
    /// <summary>Asks the provider how much storage the account uses.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The usage, or why the provider could not say. A provider that gives no quota is <see cref="ProviderError.Unexpected"/>, never a usage of zero.</returns>
    Task<Result<StorageUsage, ProviderError>> RefreshAsync(AccountId accountId, CancellationToken cancellationToken);
}
