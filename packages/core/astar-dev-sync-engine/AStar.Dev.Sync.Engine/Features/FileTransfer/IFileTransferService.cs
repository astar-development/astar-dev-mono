using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Features.FileOperations;

namespace AStar.Dev.Sync.Engine.Features.FileTransfer;

/// <summary>
///     Abstracts download and upload operations for a single file transfer slot (SE-02).
///     Registered as <c>Transient</c> — one instance per transfer slot.
/// </summary>
public interface IFileTransferService
{
    /// <summary>Downloads a remote file to the local file system.</summary>
    Task<Result<FileDownloadResult, string>> DownloadAsync(string accessToken, string remoteFileId, string localPath, IProgress<long>? progress, CancellationToken ct = default);

    /// <summary>Uploads a local file to OneDrive.</summary>
    Task<Result<FileUploadResult, string>> UploadAsync(string accessToken, string localPath, string remoteFolderId, IProgress<long>? progress, CancellationToken ct = default);
}
