using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Gets a Microsoft Graph access token for a stored OneDrive account without user interaction. The seam the sync adapters use.</summary>
public interface IAccessTokenSource
{
    /// <summary>Gets a token for the account from the token cache, refreshing it when needed.</summary>
    /// <param name="accountId">The stored account to get a token for.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns><see cref="AccessTokenOutcome.Acquired"/>, or why no token is available.</returns>
    Task<AccessTokenOutcome> GetAccessTokenAsync(AccountId accountId, CancellationToken cancellationToken);
}
