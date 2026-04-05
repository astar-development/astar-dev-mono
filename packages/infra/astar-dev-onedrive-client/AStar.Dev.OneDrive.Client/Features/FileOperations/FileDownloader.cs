using AStar.Dev.Functional.Extensions;
using AStar.Dev.Logging.Extensions;
using AStar.Dev.OneDrive.Client.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;
using System.IO.Abstractions;

namespace AStar.Dev.OneDrive.Client.Features.FileOperations;

/// <inheritdoc />
internal sealed class FileDownloader(IGraphClientFactory graphClientFactory, IFileSystem fileSystem, ILogger<FileDownloader> logger) : IFileDownloader
{
    private const int BufferSize = 81920;

    /// <inheritdoc />
    public async Task<Result<FileDownloadResult, string>> DownloadAsync(string accessToken, string remoteFileId, string localPath, IProgress<long>? progress, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteFileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(localPath);

        using var client = graphClientFactory.Create(accessToken);

        try
        {
            var drive = await GraphRetryHelper.CallWithRetryAsync(() => client.Me.Drive.GetAsync(cancellationToken: ct), logger, ct).ConfigureAwait(false);

            if (drive?.Id is null)
                return new Result<FileDownloadResult, string>.Error("Could not resolve OneDrive drive ID for the account.");

            var contentStream = await GraphRetryHelper.CallWithRetryAsync(() => client.Drives[drive.Id].Items[remoteFileId].Content.GetAsync(cancellationToken: ct), logger, ct).ConfigureAwait(false);

            if (contentStream is null)
                return new Result<FileDownloadResult, string>.Error($"Graph returned no content stream for item '{remoteFileId}'.");

            var directory = fileSystem.Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory) && !fileSystem.Directory.Exists(directory))
                fileSystem.Directory.CreateDirectory(directory);

            return await WriteStreamToFileAsync(contentStream, localPath, progress, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            DeletePartialFile(localPath);
            throw;
        }
        catch (ODataError oDataError) when (oDataError.ResponseStatusCode == 429)
        {
            DeletePartialFile(localPath);

            return new Result<FileDownloadResult, string>.Error("Graph API throttled: maximum retries exceeded.");
        }
        catch (ODataError oDataError)
        {
            DeletePartialFile(localPath);

            return new Result<FileDownloadResult, string>.Error($"Graph API error downloading '{remoteFileId}': {oDataError.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            DeletePartialFile(localPath);

            return new Result<FileDownloadResult, string>.Error($"Unexpected error downloading '{remoteFileId}': {ex.Message}");
        }
    }

    private async Task<Result<FileDownloadResult, string>> WriteStreamToFileAsync(Stream contentStream, string localPath, IProgress<long>? progress, CancellationToken ct)
    {
        using var disposableStream = contentStream;
        using var fileStream = fileSystem.FileStream.New(localPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer       = new byte[BufferSize];
        var totalWritten = 0L;
        int bytesRead;

        while ((bytesRead = await disposableStream.ReadAsync(buffer.AsMemory(0, BufferSize), ct).ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
            totalWritten += bytesRead;
            progress?.Report(totalWritten);
        }

        LogMessage.FileDownloaded(logger, totalWritten, localPath);

        return new Result<FileDownloadResult, string>.Ok(FileDownloadResultFactory.Create(localPath, totalWritten));
    }

    private void DeletePartialFile(string localPath)
    {
        try
        {
            if (fileSystem.File.Exists(localPath))
                fileSystem.File.Delete(localPath);
        }
        catch (IOException ex)
        {
            LogMessage.PartialFileDeletionFailed(logger, ex, localPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogMessage.PartialFileDeletionFailed(logger, ex, localPath);
        }
    }
}
