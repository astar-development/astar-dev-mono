using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Client.Features.Authentication;

/// <summary>
///     Manages OAuth token lifecycle: acquisition, refresh, persistence, and failure handling (AU-02, AU-04, AU-05, NF-06).
///     Scoped per account — each account has its own token state machine.
///     All methods return <c>Result&lt;T, string&gt;</c> or <c>Option&lt;T&gt;</c> (NF-16).
/// </summary>
public interface ITokenManager
{
    /// <summary>
    ///     Account ID this token manager is bound to.
    /// </summary>
    Guid AccountId { get; }

    /// <summary>
    ///     Get a valid access token, attempting silent refresh if needed (AU-04).
    ///     Returns <c>Failure</c> if refresh fails or user must re-authenticate.
    /// </summary>
    /// <remarks>
    ///     On failure, the auth state transitions to <c>AuthRequired</c> and
    ///     <see cref="IAuthStateService.AccountAuthStateChanged" /> is published.
    /// </remarks>
    Task<Result<AccessToken, string>> GetTokenSilentlyAsync(CancellationToken ct = default);

    /// <summary>
    ///     Persist a newly-acquired access token (e.g. after interactive auth flow).
    /// </summary>
    Task<Result<bool, string>> PersistTokenAsync(AccessToken token, string? refreshToken, CancellationToken ct = default);

    /// <summary>
    ///     Clear all stored tokens for this account (e.g. on logout).
    /// </summary>
    Task<Result<bool, string>> ClearTokenAsync(CancellationToken ct = default);
}
