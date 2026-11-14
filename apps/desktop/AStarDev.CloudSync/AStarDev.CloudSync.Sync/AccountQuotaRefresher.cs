using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="refreshers">Finds the provider's quota reader.</param>
/// <param name="accountStore">Saves the usage.</param>
/// <param name="logger">Reports a refresh that failed, by error type only.</param>
internal sealed class AccountQuotaRefresher(IProviderCapability<IQuotaRefresher> refreshers, IAccountStore accountStore, ILogger<AccountQuotaRefresher> logger) : IAccountQuotaRefresher
{
    /// <inheritdoc />
    public Task<Result<StorageUsage, ProviderError>> RefreshAsync(AccountId accountId, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{refreshers} {accountStore} {logger} {accountId} {cancellationToken}");
}
