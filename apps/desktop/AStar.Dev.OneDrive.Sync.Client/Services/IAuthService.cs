namespace AStar.Dev.OneDrive.Sync.Client.Services;

/// <summary>
/// Abstracts MSAL authentication for OneDrive personal accounts.
/// All operations are cancellable and never throw — failures are
/// returned as <see cref="AuthResult.Failure"/> values.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Launches the system browser for interactive sign-in.
    /// Returns when the user completes or cancels authentication.
    /// </summary>
    Task<AuthResult> SignInInteractiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Attempts a silent token refresh for an already-authenticated account.
    /// Does NOT open a browser — returns <see cref="AuthResult.Failure"/> if
    /// the token cannot be refreshed silently.
    /// </summary>
    Task<AuthResult> AcquireTokenSilentAsync(string accountId, CancellationToken ct = default);

    /// <summary>
    /// Removes the account from the MSAL cache and deletes cached tokens.
    /// </summary>
    Task SignOutAsync(string accountId, CancellationToken ct = default);

    /// <summary>
    /// Returns all account IDs currently held in the token cache.
    /// Used at startup to discover which accounts have cached tokens.
    /// </summary>
    Task<IReadOnlyList<string>> GetCachedAccountIdsAsync();
}
