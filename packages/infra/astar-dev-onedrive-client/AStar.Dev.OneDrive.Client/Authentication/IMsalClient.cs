using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Client.Authentication;

/// <summary>
///     Abstracts MSAL public-client operations to decouple application logic
///     from the MSAL builder API and enable unit testing.
/// </summary>
public interface IMsalClient
{
    /// <summary>
    ///     Returns all accounts currently held in the MSAL token cache.
    /// </summary>
    Task<IEnumerable<IAccount>> GetAccountsAsync();

    /// <summary>
    ///     Attempts to acquire an access token silently from the cache (AU-04).
    /// </summary>
    /// <param name="scopes">The requested OAuth2 scopes.</param>
    /// <param name="account">The account hint; pass <c>null</c> to use any cached account.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token string.</returns>
    /// <exception cref="Microsoft.Identity.Client.MsalUiRequiredException">
    ///     Thrown when the cache does not contain a valid token and user interaction is required.
    /// </exception>
    Task<string> AcquireTokenSilentAsync(
        IEnumerable<string> scopes,
        IAccount?           account,
        CancellationToken   cancellationToken = default);

    /// <summary>
    ///     Acquires an access token via interactive browser sign-in (AU-04).
    /// </summary>
    /// <param name="scopes">The requested OAuth2 scopes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token string.</returns>
    Task<string> AcquireTokenInteractiveAsync(
        IEnumerable<string> scopes,
        CancellationToken   cancellationToken = default);

    /// <summary>
    ///     Removes the specified account from the MSAL token cache.
    /// </summary>
    /// <param name="account">The account to remove.</param>
    Task RemoveAccountAsync(IAccount account);
}
