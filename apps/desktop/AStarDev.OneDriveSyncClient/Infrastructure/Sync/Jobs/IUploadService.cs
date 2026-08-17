using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using Microsoft.Graph;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;

public interface IUploadService
{
    /// <summary>
    /// Uploads a local file to OneDrive using a resumable upload session.
    /// Returns the uploaded DriveItem ID on success.
    /// </summary>
    Task<Result<string, string>> UploadAsync(GraphServiceClient client, DriveId driveId, string parentFolderId, string localPath, string remotePath, IProgress<long>? progress = null, CancellationToken cancellationToken = default);
}
