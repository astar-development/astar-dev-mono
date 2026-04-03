using AStar.Dev.Functional.Extensions;
using AStar.Dev.Logging.Extensions;
using AStar.Dev.OneDrive.Client.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using System.IO.Abstractions;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AStar.Dev.OneDrive.Client.Features.FileOperations;

/// <inheritdoc />
internal sealed class FileUploader(IGraphClientFactory graphClientFactory, IFileSystem fileSystem, IHttpClientFactory httpClientFactory, ILogger<FileUploader> logger) : IFileUploader
{
    private const long DirectUploadThresholdBytes = 4 * 1024 * 1024;
    private const int ChunkSizeBytes = 5 * 1024 * 1024;

    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public async Task<Result<FileUploadResult, string>> UploadAsync(string accessToken, string localPath, string remoteFolderId, IProgress<long>? progress, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(localPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteFolderId);

        if (!fileSystem.File.Exists(localPath))
            return new Result<FileUploadResult, string>.Error($"Local file not found: {localPath}");

        using var client = graphClientFactory.Create(accessToken);

        try
        {
            var drive = await client.Me.Drive.GetAsync(cancellationToken: ct).ConfigureAwait(false);

            if (drive?.Id is null)
                return new Result<FileUploadResult, string>.Error("Could not resolve OneDrive drive ID for the account.");

            var fileInfo = fileSystem.FileInfo.New(localPath);
            var fileName = fileInfo.Name;

            return fileInfo.Length <= DirectUploadThresholdBytes
                ? await DirectUploadAsync(client, drive.Id, remoteFolderId, fileName, localPath, fileInfo.Length, progress, ct).ConfigureAwait(false)
                : await ChunkedUploadAsync(client, drive.Id, remoteFolderId, fileName, localPath, fileInfo.Length, progress, ct).ConfigureAwait(false);
        }
        catch (ODataError oDataError)
        {
            return new Result<FileUploadResult, string>.Error($"Graph API error uploading '{localPath}': {oDataError.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Result<FileUploadResult, string>.Error($"Unexpected error uploading '{localPath}': {ex.Message}");
        }
    }

    private async Task<Result<FileUploadResult, string>> DirectUploadAsync(Microsoft.Graph.GraphServiceClient client, string driveId, string remoteFolderId, string fileName, string localPath, long fileSize, IProgress<long>? progress, CancellationToken ct)
    {
        using var fileStream  = fileSystem.FileStream.New(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var uploadPath        = $"{remoteFolderId}:/{fileName}:";
        var uploadedItem      = await client.Drives[driveId].Items[uploadPath].Content.PutAsync(fileStream, cancellationToken: ct).ConfigureAwait(false);

        if (uploadedItem?.Id is null)
            return new Result<FileUploadResult, string>.Error($"Graph did not return an item ID after uploading '{fileName}'.");

        progress?.Report(fileSize);
        LogMessage.FileUploaded(logger, fileName, fileSize, uploadedItem.Id);

        return new Result<FileUploadResult, string>.Ok(FileUploadResultFactory.Create(uploadedItem.Id, fileName, fileSize));
    }

    private async Task<Result<FileUploadResult, string>> ChunkedUploadAsync(Microsoft.Graph.GraphServiceClient client, string driveId, string remoteFolderId, string fileName, string localPath, long fileSize, IProgress<long>? progress, CancellationToken ct)
    {
        var uploadPath = $"{remoteFolderId}:/{fileName}:";
        var session    = await client.Drives[driveId].Items[uploadPath].CreateUploadSession.PostAsync(new CreateUploadSessionPostRequestBody(), cancellationToken: ct).ConfigureAwait(false);

        if (session?.UploadUrl is null)
            return new Result<FileUploadResult, string>.Error($"Graph did not return an upload session URL for '{fileName}'.");

        return await UploadChunksAsync(session.UploadUrl, localPath, fileName, fileSize, progress, ct).ConfigureAwait(false);
    }

    private async Task<Result<FileUploadResult, string>> UploadChunksAsync(string uploadUrl, string localPath, string fileName, long fileSize, IProgress<long>? progress, CancellationToken ct)
    {
        using var fileStream = fileSystem.FileStream.New(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var httpClient = httpClientFactory.CreateClient();
        var buffer           = new byte[ChunkSizeBytes];
        var offset           = 0L;
        DriveItem? finalItem = null;

        while (offset < fileSize)
        {
            var bytesRead = await fileStream.ReadAsync(buffer.AsMemory(), ct).ConfigureAwait(false);
            if (bytesRead == 0)
                break;

            using var chunkContent = new ByteArrayContent(buffer, 0, bytesRead);
            chunkContent.Headers.ContentLength = bytesRead;
            chunkContent.Headers.ContentRange  = new ContentRangeHeaderValue(offset, offset + bytesRead - 1, fileSize);

            using var request  = new HttpRequestMessage(HttpMethod.Put, uploadUrl) { Content = chunkContent };
            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode && (int)response.StatusCode != 308)
                return new Result<FileUploadResult, string>.Error($"Chunk upload failed at offset {offset}: HTTP {(int)response.StatusCode}");

            offset += bytesRead;
            progress?.Report(offset);

            if (response.IsSuccessStatusCode && (int)response.StatusCode != 308)
            {
                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                finalItem = JsonSerializer.Deserialize<DriveItem>(json, CaseInsensitiveOptions);
            }
        }

        if (finalItem?.Id is null)
            return new Result<FileUploadResult, string>.Error($"Chunked upload did not return a final item ID for '{fileName}'.");

        LogMessage.FileUploaded(logger, fileName, fileSize, finalItem.Id);

        return new Result<FileUploadResult, string>.Ok(FileUploadResultFactory.Create(finalItem.Id, fileName, fileSize));
    }
}
