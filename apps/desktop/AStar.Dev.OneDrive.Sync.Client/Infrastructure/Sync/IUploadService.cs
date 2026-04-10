using Microsoft.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

public interface IUploadService
{
    /// <summary>
    /// Uploads a local file to OneDrive using a resumable upload session.
    /// Returns the uploaded DriveItem ID on success.
    /// </summary>
    Task<string> UploadAsync(GraphServiceClient client, string driveId, string parentFolderId, string localPath, string remotePath, IProgress<long>? progress = null, CancellationToken ct = default);
}
