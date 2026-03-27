using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Client.Authentication;

/// <summary>
///     MSAL-backed implementation of <see cref="ITokenManager"/>.
///     Acquires tokens silently from the local cache (AU-02, AU-04) and only
///     initiates an interactive browser flow when the cache cannot satisfy the
///     request (AU-04).
/// </summary>
public sealed class TokenManager : ITokenManager
{
    private readonly IMsalClient _msal;

    /// <summary>
    ///     Initialises a new instance of <see cref="TokenManager"/>.
    /// </summary>
    /// <param name="msal">The MSAL client used to acquire tokens.</param>
    public TokenManager(IMsalClient msal)
        => _msal = msal;

    /// <inheritdoc />
    public async Task<string> AcquireAccessTokenAsync(IEnumerable<string> scopes, CancellationToken cancellationToken = default)
    {
        var scopeList = scopes as IReadOnlyList<string> ?? scopes.ToList();
        var accounts  = await _msal.GetAccountsAsync().ConfigureAwait(false);

        try
        {
            return await _msal.AcquireTokenSilentAsync(scopeList, accounts.FirstOrDefault(), cancellationToken)
                              .ConfigureAwait(false);
        }
        catch(MsalUiRequiredException)
        {
            return await _msal.AcquireTokenInteractiveAsync(scopeList, cancellationToken)
                              .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _msal.GetAccountsAsync().ConfigureAwait(false);

        foreach(var account in accounts)
        {
            await _msal.RemoveAccountAsync(account).ConfigureAwait(false);
        }
    }
}
