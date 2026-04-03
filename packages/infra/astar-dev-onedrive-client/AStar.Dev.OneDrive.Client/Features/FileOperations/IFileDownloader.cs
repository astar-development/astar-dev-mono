using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Client.Features.FileOperations;

/// <summary>
///     Downloads files from OneDrive to the local file system (SE-02).
///     Respects <see cref="CancellationToken"/> and cleans up partial files on cancellation.
///     All methods return <see cref="Result{TSuccess,TError}"/> — callers never see Graph exceptions.
/// </summary>
public interface IFileDownloader
{
    /// <summary>
    ///     Downloads the remote file identified by <paramref name="remoteFileId"/> to <paramref name="localPath"/>.
    ///     Progress is reported in total bytes written via <paramref name="progress"/>.
    ///     Any partial file is deleted if the operation is cancelled or fails.
    /// </summary>
    /// <remarks>
    ///     SE-16 (resumable downloads via byte-range) is Post-MVP. The interface is designed to accommodate
    ///     a <c>byteOffset</c> parameter in a future overload without breaking this signature.
    /// </remarks>
    Task<Result<FileDownloadResult, string>> DownloadAsync(string accessToken, string remoteFileId, string localPath, IProgress<long>? progress, CancellationToken ct = default);
}
