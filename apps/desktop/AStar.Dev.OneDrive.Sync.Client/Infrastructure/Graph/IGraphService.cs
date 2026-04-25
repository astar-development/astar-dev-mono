using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

public interface IGraphService
{
    /// <summary>Returns the ID of the user's default drive (OneDrive for Business or Personal).</summary>
    Task<string> GetDriveIdAsync(string accessToken, CancellationToken ct = default);

    /// <summary>Returns the immediate child folders of the root folder.</summary>
    Task<List<DriveFolder>> GetRootFoldersAsync(string accessToken, CancellationToken ct = default);

    /// <summary>Returns the immediate child folders of the given parent folder. Used for lazy-loading the folder tree.</summary>
    Task<List<DriveFolder>> GetChildFoldersAsync(string accessToken, string driveId, string parentFolderId, CancellationToken ct = default);

    /// <summary>Returns the user's OneDrive storage quota.</summary>
    Task<(long Total, long Used)> GetQuotaAsync(string accessToken, CancellationToken ct = default);

    /// <summary>Enumerates all descendants (files and folders) of the given folder. ETag and CTag are populated on each returned DeltaItem.</summary>
    Task<List<DeltaItem>> EnumerateFolderAsync(string accessToken, string driveId, string folderId, string remotePath, CancellationToken ct = default);

    /// <summary>Resolves the OneDrive item ID for a path relative to the drive root. Returns null if the path does not exist.</summary>
    Task<string?> GetFolderIdByPathAsync(string accessToken, string driveId, string remotePath, CancellationToken ct = default);

    /// <summary>Fetches the pre-authenticated download URL for a specific drive item.</summary>
    Task<string?> GetDownloadUrlAsync(string accessToken, string itemId, CancellationToken ct = default);

    /// <summary>Uploads a local file to OneDrive using a resumable upload session. Returns the remote item ID on success.</summary>
    Task<string> UploadFileAsync(string accessToken, string localPath, string remotePath, string parentFolderId, CancellationToken ct = default);

    /// <summary>Permanently deletes the specified item from OneDrive (moves it to the recycle bin).</summary>
    Task DeleteItemAsync(string accessToken, string itemId, CancellationToken ct = default);
}
