namespace AStar.Dev.Sync.Engine;

/// <summary>
/// Abstracts remote file-storage operations so the sync engine
/// is decoupled from any specific cloud provider.
/// </summary>
public interface ISyncProvider
{
    /// <summary>
    /// Returns the set of items that differ between local and remote
    /// for the given account, filtered to the selected folders (SE-07).
    /// </summary>
    /// <param name="accountId">The account to query.</param>
    /// <param name="selectedFolders">Folder paths to include.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<SyncItem>> GetChangesAsync(string accountId, IReadOnlyList<string> selectedFolders, CancellationToken ct = default);

    /// <summary>
    /// Uploads a local file to the remote provider.
    /// </summary>
    /// <param name="accountId">The account to upload to.</param>
    /// <param name="item">The item to upload.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<SyncItemResult> UploadAsync(string accountId, SyncItem item, CancellationToken ct = default);

    /// <summary>
    /// Downloads a remote file to the local file system.
    /// </summary>
    /// <param name="accountId">The account to download from.</param>
    /// <param name="item">The item to download.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<SyncItemResult> DownloadAsync(string accountId, SyncItem item, CancellationToken ct = default);
}
