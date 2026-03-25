namespace AStar.Dev.OneDrive.Client.Authentication;

/// <summary>
///     Manages access-token acquisition for OneDrive API calls.
///     Tokens are refreshed silently whenever possible; user interaction
///     is requested only when the silent path fails (AU-04).
/// </summary>
public interface ITokenManager
{
    /// <summary>
    ///     Returns a valid access token for the requested <paramref name="scopes"/>.
    ///     Attempts silent acquisition first; falls back to interactive sign-in
    ///     only when the cached token is absent or expired (AU-04).
    /// </summary>
    /// <param name="scopes">The OAuth2 scopes to request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A valid access token string.</returns>
    Task<string> AcquireAccessTokenAsync(IEnumerable<string> scopes, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Signs out all cached accounts, clearing the local token cache.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SignOutAsync(CancellationToken cancellationToken = default);
}
