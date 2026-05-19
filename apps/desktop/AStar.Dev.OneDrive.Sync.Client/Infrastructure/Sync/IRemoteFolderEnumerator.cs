using AStar.Dev.OneDrive.Sync.Client.Accounts;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Loads sync rules for an account, resolves drive and folder IDs, and enumerates
/// remote delta items — returning raw results for downstream processing.
/// </summary>
public interface IRemoteFolderEnumerator
{
    /// <summary>
    /// Performs a full remote enumeration pass for <paramref name="account"/>,
    /// returning the raw delta-item list alongside seen IDs and rules.
    /// </summary>
    Task<RemoteEnumerationResult> EnumerateAsync(OneDriveAccount account, string accessToken, CancellationToken ct);
}
