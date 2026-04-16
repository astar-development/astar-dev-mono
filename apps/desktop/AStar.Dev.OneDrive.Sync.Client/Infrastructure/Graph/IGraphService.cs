using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

public interface IGraphService
{
    /// <summary>
    /// Returns the ID of the user's default drive (OneDrive for Business or Personal).
    /// </summary>
    /// <param name="accessToken">The access token for the authenticated user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The drive ID. (which is cached to reduce calls)</returns>
    Task<string> GetDriveIdAsync(string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Returns the immediate child folders of the root folder.
    /// </summary>
    /// <param name="accessToken">The access token for the authenticated user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The list of child folders.</returns>
    Task<List<DriveFolder>> GetRootFoldersAsync(string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Returns the immediate child folders of the given parent folder.
    /// Used for lazy-loading the folder tree.
    /// </summary>
    /// <param name="accessToken">The access token for the authenticated user.</param>
    /// <param name="driveId">The ID of the drive.</param>
    /// <param name="parentFolderId">The ID of the parent folder.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The list of child folders.</returns>
    Task<List<DriveFolder>> GetChildFoldersAsync(string accessToken, string driveId, string parentFolderId, CancellationToken ct = default);

    /// <summary>Returns the user's OneDrive storage quota.</summary>
    /// <param name="accessToken">The access token for the authenticated user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The total and used storage space.</returns>
    Task<(long Total, long Used)> GetQuotaAsync(string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Returns the changes (delta) since the last sync, or all items if no delta link is provided.
    /// </summary>
    /// <param name="accessToken">The access token for the authenticated user.</param>
    /// <param name="folderId">The ID of the folder to query for changes.</param>
    /// <param name="deltaLink">The delta link for incremental sync.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The delta result containing the changes.</returns>
    Task<DeltaResult> GetDeltaAsync(string accessToken, string folderId, string? deltaLink, CancellationToken ct = default);

    /// <summary>
    /// Fetches the pre-authenticated download URL for a specific drive item.
    /// Use this when <c>@microsoft.graph.downloadUrl</c> is absent from a delta or children response.
    /// </summary>
    /// <param name="accessToken">The access token for the authenticated user.</param>
    /// <param name="itemId">The Graph item ID of the file.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The download URL, or <see langword="null"/> if not available.</returns>
    Task<string?> GetDownloadUrlAsync(string accessToken, string itemId, CancellationToken ct = default);

    /// <summary>
    /// Uploads a local file to OneDrive using a resumable upload session.
    /// Handles all file sizes. Returns the remote item ID on success.
    /// </summary>
    Task<string> UploadFileAsync(string accessToken, string localPath, string remotePath, string parentFolderId, CancellationToken ct = default);
}
