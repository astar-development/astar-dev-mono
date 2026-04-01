using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Client.Features.Authentication;

/// <summary>
///     Abstraction over MSAL (Microsoft Authentication Library) for OAuth flows (AU-01).
///     All implementations must use <c>ConfigureAwait(false)</c> on all <c>await</c> calls.
/// </summary>
public interface IMsalClient
{
    /// <summary>
    ///     Acquire an access token via the system browser interactive flow (AU-01).
    ///     Scopes requested: Files.ReadWrite, offline_access.
    ///     Must not be called on the Avalonia UI thread — dispatch via <c>Task.Run</c>.
    /// </summary>
    /// <returns>Access token and refresh token (if offline_access granted).</returns>
    Task<Result<(AccessToken, string?), string>> AcquireTokenInteractiveAsync(CancellationToken ct = default);

    /// <summary>
    ///     Attempt silent token refresh using cached tokens (AU-04).
    ///     Never throws on failure — returns <see cref="Result{TSuccess,TError}"/> with failure instead.
    /// </summary>
    /// <returns><c>Success</c> if refresh succeeded; <c>Failure</c> if user auth required or network error.</returns>
    Task<Result<AccessToken, string>> AcquireTokenSilentAsync(CancellationToken ct = default);
}
