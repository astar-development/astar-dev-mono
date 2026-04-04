using System.Diagnostics.CodeAnalysis;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Features.FileOperations;

namespace AStar.Dev.Sync.Engine.Features.FileTransfer;

/// <inheritdoc />
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Registered via DI in SyncEngineServiceExtensions.")]
internal sealed class FileTransferService(IFileDownloader downloader, IFileUploader uploader) : IFileTransferService
{
    /// <inheritdoc />
    public Task<Result<FileDownloadResult, string>> DownloadAsync(string accessToken, string remoteFileId, string localPath, IProgress<long>? progress, CancellationToken ct = default)
        => downloader.DownloadAsync(accessToken, remoteFileId, localPath, progress, ct);

    /// <inheritdoc />
    public Task<Result<FileUploadResult, string>> UploadAsync(string accessToken, string localPath, string remoteFolderId, IProgress<long>? progress, CancellationToken ct = default)
        => uploader.UploadAsync(accessToken, localPath, remoteFolderId, progress, ct);
}
