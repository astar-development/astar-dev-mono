using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Client.Features.Authentication;

/// <summary>
///     Records and retrieves per-account consent decisions for insecure token fallback storage (AU-02, AU-03, NF-06).
/// </summary>
public interface IConsentStore
{
    /// <summary>
    ///     Check if user has already made a consent decision for this account.
    /// </summary>
    Task<Option<bool>> GetConsentDecisionAsync(Guid accountId, CancellationToken ct = default);

    /// <summary>
    ///     Record user's consent decision (allow or deny insecure fallback token storage).
    /// </summary>
    Task<Result<bool, string>> SetConsentDecisionAsync(Guid accountId, bool consented, CancellationToken ct = default);
}
