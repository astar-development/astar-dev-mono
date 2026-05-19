using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using System.Reactive;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

public interface IGraphService
{
    /// <summary>Returns the ID of the user's default drive (OneDrive for Business or Personal).</summary>
    Task<Result<DriveId, string>> GetDriveIdAsync(string accountId, string accessToken, CancellationToken ct = default);

    /// <summary>Returns the immediate child folders of the root folder.</summary>
    Task<Result<List<DriveFolder>, string>> GetRootFoldersAsync(string accountId, string accessToken, CancellationToken ct = default);

    /// <summary>Returns the immediate child folders of the given parent folder. Used for lazy-loading the folder tree.</summary>
    Task<Result<List<DriveFolder>, string>> GetChildFoldersAsync(string accessToken, DriveId driveId, string parentFolderId, CancellationToken ct = default);

    /// <summary>Returns the user's OneDrive storage quota.</summary>
    Task<Result<(long Total, long Used), string>> GetQuotaAsync(string accountId, string accessToken, CancellationToken ct = default);

    /// <summary>Enumerates all descendants (files and folders) of the given folder. ETag and CTag are populated on each returned DeltaItem.</summary>
    Task<Result<List<DeltaItem>, string>> EnumerateFolderAsync(string accessToken, DriveId driveId, string folderId, string remotePath, CancellationToken ct = default);

    /// <summary>Resolves the OneDrive item ID for a path relative to the drive root. Returns null if the path does not exist.</summary>
    Task<string?> GetFolderIdByPathAsync(string accessToken, DriveId driveId, string remotePath, CancellationToken ct = default);

    /// <summary>Fetches the pre-authenticated download URL for a specific drive item.</summary>
    Task<Result<string, string>> GetDownloadUrlAsync(string accountId, string accessToken, string itemId, CancellationToken ct = default);

    /// <summary>Uploads a local file to OneDrive using a resumable upload session. Returns the remote item ID on success.</summary>
    Task<Result<string, string>> UploadFileAsync(string accountId, string accessToken, string localPath, string remotePath, string parentFolderId, CancellationToken ct = default);

    /// <summary>Permanently deletes the specified item from OneDrive (moves it to the recycle bin).</summary>
    Task<Result<Unit, string>> DeleteItemAsync(string accountId, string accessToken, string itemId, CancellationToken ct = default);

    /// <summary>Removes the cached drive context for the given account. Call after sign-out to prevent stale entries accumulating.</summary>
    void EvictCachedDriveContext(string accountId);
}
