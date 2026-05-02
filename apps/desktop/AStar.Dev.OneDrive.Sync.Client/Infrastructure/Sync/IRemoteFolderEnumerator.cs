using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Loads sync rules and synced items for an account, iterates every root include rule,
/// resolves folder IDs, enumerates remote items, and classifies each as skipped, folder,
/// phantom, conflict, or download job.
/// </summary>
public interface IRemoteFolderEnumerator
{
    /// <summary>
    /// Performs a full remote enumeration pass for <paramref name="account"/>.
    /// Invokes <paramref name="onConflict"/> for each conflict detected so the caller
    /// can persist and raise the event without coupling this service to <c>ISyncRepository</c>.
    /// </summary>
    Task<RemoteEnumerationResult> EnumerateAsync(OneDriveAccount account, string accessToken, Func<SyncConflict, Task> onConflict, CancellationToken ct);
}
