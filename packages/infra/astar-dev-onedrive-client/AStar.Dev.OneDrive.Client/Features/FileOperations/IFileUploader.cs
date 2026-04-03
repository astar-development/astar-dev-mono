using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Client.Features.FileOperations;

/// <summary>
///     Uploads local files to OneDrive via the Graph API.
///     Uses direct PUT for files ≤ 4 MB; chunked upload sessions for larger files.
///     All methods return <see cref="Result{TSuccess,TError}"/> — callers never see Graph exceptions.
/// </summary>
public interface IFileUploader
{
    /// <summary>
    ///     Uploads the file at <paramref name="localPath"/> into the remote folder <paramref name="remoteFolderId"/>.
    ///     Progress is reported in total bytes uploaded via <paramref name="progress"/>.
    /// </summary>
    Task<Result<FileUploadResult, string>> UploadAsync(string accessToken, string localPath, string remoteFolderId, IProgress<long>? progress, CancellationToken ct = default);
}
