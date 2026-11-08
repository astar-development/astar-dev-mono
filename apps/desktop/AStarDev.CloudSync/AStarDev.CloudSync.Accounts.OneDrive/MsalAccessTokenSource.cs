using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Gets tokens from the MSAL token cache for accounts saved in the account store.</summary>
/// <param name="tokenCache">Connects the token cache before it is read.</param>
/// <param name="accountStore">Maps the account id to the provider key MSAL knows.</param>
/// <param name="msal">Reads the token silently.</param>
/// <param name="logger">Reports tokens that cannot be obtained.</param>
internal sealed class MsalAccessTokenSource(TokenCacheInitialiser tokenCache, IAccountStore accountStore, IMsalClient msal, ILogger<MsalAccessTokenSource> logger) : IAccessTokenSource
{
    /// <inheritdoc />
    public Task<AccessTokenOutcome> GetAccessTokenAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{tokenCache}{accountStore}{msal}{logger}{accountId}{cancellationToken}");
}
