using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Refreshes an account's stored storage usage through its provider's <see cref="IQuotaRefresher"/>. A sync run calls it after each pass (#1565).</summary>
public interface IAccountQuotaRefresher
{
    /// <summary>Reads the account's usage from its provider and saves it. When the provider cannot say, or the save fails, the last saved value stays and a warning is logged.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the refresh.</param>
    /// <returns>The saved usage, or why it could not be refreshed.</returns>
    Task<Result<StorageUsage, ProviderError>> RefreshAsync(AccountId accountId, CancellationToken cancellationToken);
}
