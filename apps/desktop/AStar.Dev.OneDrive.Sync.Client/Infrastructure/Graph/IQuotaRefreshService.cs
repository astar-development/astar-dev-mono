using AStar.Dev.OneDrive.Sync.Client.Accounts;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

/// <summary>
/// Fetches the current OneDrive storage quota for an account from the Graph API,
/// updates the in-memory account object, and persists the result to the database.
/// All failures are swallowed — callers must not depend on success.
/// </summary>
public interface IQuotaRefreshService
{
    /// <summary>Refreshes the quota for <paramref name="account"/>. Updates <see cref="OneDriveAccount.Quota"/> and persists on success. No-op on failure.</summary>
    Task TryRefreshAsync(OneDriveAccount account, CancellationToken ct = default);
}
