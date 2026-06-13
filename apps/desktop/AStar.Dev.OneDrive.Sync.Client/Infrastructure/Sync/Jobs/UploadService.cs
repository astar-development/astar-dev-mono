using System.Globalization;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Http;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

/// <summary>
/// Handles resumable upload sessions for all file sizes.
///
/// Graph API resumable upload flow:
///   1. POST /drives/{id}/items/{parentId}:/{filename}:/createUploadSession
///   2. PUT chunks to the session URL until complete
///   3. On 429 or network error — retry the current chunk with backoff
///   4. On session expiry (404 on chunk PUT) — restart from step 1
///
/// Chunk size: 10 MB (must be a multiple of 320 KB per Graph API requirement).
/// </summary>
public sealed class UploadService(IHttpClientFactory httpClientFactory, IFileSystem fileSystem, ILogger<UploadService> logger) : IUploadService
{
    private const int ChunkSize10Mb = 10 * 1024 * 1024;
    private const string UploadCompletedWithoutItemIdError = "Upload completed without receiving item ID from Graph API.";

    /// <summary>
    /// Uploads a local file to OneDrive using a resumable upload session.
    /// Returns the uploaded DriveItem ID on success.
    /// </summary>
    public async Task<Result<string, string>> UploadAsync(GraphServiceClient client, DriveId driveId, string parentFolderId, string localPath, string remotePath, IProgress<long>? progress = null, CancellationToken ct = default)
    {
        var fileInfo = fileSystem.FileInfo.New(localPath);
        if (!fileInfo.Exists)
            return new Result<string, string>.Error($"Local file not found: {localPath}");

        OneDriveSyncClientMessages.UploadServiceStarting(logger, remotePath, fileInfo.Length / (1024.0 * 1024));

        var sessionResult = await CreateSessionWithRetryAsync(client, driveId.Value, parentFolderId, remotePath, fileInfo.LastWriteTimeUtc, ct).ConfigureAwait(false);

        return await sessionResult.MatchAsync(
            async sessionUrl =>
            {
                var result = await UploadChunksAsync(sessionUrl, localPath, fileInfo.Length, progress, ct).ConfigureAwait(false);

                return result.Tap(_ => OneDriveSyncClientMessages.UploadServiceCompleted(logger, remotePath));
            },
            error => new Result<string, string>.Error(error)).ConfigureAwait(false);
    }

    private static async Task<Result<string, string>> CreateSessionWithRetryAsync(GraphServiceClient client, string driveId, string parentFolderId, string remotePath, DateTime lastModified, CancellationToken ct)
    {
        string fileName = remotePath.Contains('/')
            ? remotePath[(remotePath.LastIndexOf('/') + 1)..]
            : remotePath;

        var requestBody = new CreateUploadSessionPostRequestBody
        {
            Item = new DriveItemUploadableProperties
            {
                AdditionalData = new Dictionary<string, object>
                {
                    { "@microsoft.graph.conflictBehavior", "replace" },
                    { "name", fileName },
                    { "fileSystemInfo", new
                        {
                            lastModifiedDateTime = lastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                        }
                    }
                }
            }
        };

        var session = await client
            .Drives[driveId]
            .Items[parentFolderId]
            .ItemWithPath(remotePath)
            .CreateUploadSession
            .PostAsync(requestBody, cancellationToken: ct).ConfigureAwait(false);

        if (session?.UploadUrl is null)
            return new Result<string, string>.Error("Graph API did not return an upload session URL.");

        return new Result<string, string>.Ok(session.UploadUrl);
    }

    private async Task<Result<string, string>> UploadChunksAsync(string sessionUrl, string localPath, long totalBytes, IProgress<long>? progress, CancellationToken ct)
    {
        using var http = httpClientFactory.CreateClient();
        await using var file = fileSystem.File.OpenRead(localPath);
        byte[] buffer = new byte[ChunkSize10Mb];

        return await UploadNextChunkAsync(0L).ConfigureAwait(false);

        async Task<Result<string, string>> UploadNextChunkAsync(long uploaded)
        {
            if (uploaded >= totalBytes)
                return new Result<string, string>.Error(UploadCompletedWithoutItemIdError);

            ct.ThrowIfCancellationRequested();

            int bytesRead = await ReadChunkAsync(file, buffer, totalBytes, uploaded, ct).ConfigureAwait(false);

            if (bytesRead == 0)
                return new Result<string, string>.Error(UploadCompletedWithoutItemIdError);

            long rangeEnd = ComputeRangeEnd(uploaded, bytesRead);

            return await UploadChunkWithRetryAsync(http, sessionUrl, buffer.AsMemory(0, bytesRead), uploaded, rangeEnd, totalBytes, ct)
                .BindAsync(async itemId =>
                {
                    long newUploaded = uploaded + bytesRead;
                    progress?.Report(newUploaded);

                    if (itemId is not null)
                        return new Result<string, string>.Ok(itemId);

                    return await UploadNextChunkAsync(newUploaded).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }
    }

    private static async Task<int> ReadChunkAsync(Stream file, byte[] buffer, long totalBytes, long uploaded, CancellationToken ct)
    {
        int bytesToRead = (int)Math.Min(ChunkSize10Mb, totalBytes - uploaded);

        return await file.ReadAsync(buffer.AsMemory(0, bytesToRead), ct).ConfigureAwait(false);
    }

    private static long ComputeRangeEnd(long uploaded, int bytesRead) => uploaded + bytesRead - 1;

    private async Task<Result<string?, string>> UploadChunkWithRetryAsync(HttpClient http, string sessionUrl, ReadOnlyMemory<byte> chunk, long rangeStart, long rangeEnd, long totalBytes, CancellationToken ct)
    {
        int attempt = 0;

        while (true)
        {
            attempt++;
            ct.ThrowIfCancellationRequested();

            try
            {
                var array = MemoryMarshal.TryGetArray(chunk, out var segment) ? segment : new ArraySegment<byte>(chunk.ToArray());
                using var content = new ByteArrayContent(array.Array!, array.Offset, array.Count);
                content.Headers.Add("Content-Range", $"bytes {rangeStart}-{rangeEnd}/{totalBytes}");
                content.Headers.Add("Content-Length", chunk.Length.ToString(CultureInfo.CurrentCulture));

                using var response = await http.PutAsync(sessionUrl, content, ct).ConfigureAwait(false);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    if (attempt > HttpRetryPolicy.MaxRetries)
                        return new Result<string?, string>.Error($"Upload rate limited after {HttpRetryPolicy.MaxRetries} retries.");

                    var delay = HttpRetryPolicy.GetRetryDelay(response, attempt);
                    OneDriveSyncClientMessages.UploadChunkThrottled(logger, rangeStart, rangeEnd, delay.TotalSeconds, attempt, HttpRetryPolicy.MaxRetries);

                    await Task.Delay(delay, ct).ConfigureAwait(false);
                    continue;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                    return new Result<string?, string>.Ok(null);

                if (response.StatusCode is System.Net.HttpStatusCode.Created or System.Net.HttpStatusCode.OK)
                    return await GetUploadedDocumentId(response, ct).ConfigureAwait(false);

                _ = response.EnsureSuccessStatusCode();

                return new Result<string?, string>.Ok(null);
            }
            catch (HttpRequestException) when (attempt <= HttpRetryPolicy.MaxRetries)
            {
                var delay = HttpRetryPolicy.GetBackoffDelay(attempt);
                OneDriveSyncClientMessages.UploadChunkNetworkError(logger, rangeStart, rangeEnd, delay.TotalSeconds, attempt, HttpRetryPolicy.MaxRetries);

                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
    }

    private static async Task<Result<string?, string>> GetUploadedDocumentId(HttpResponseMessage response, CancellationToken ct)
    {
        string json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = System.Text.Json.JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("id", out var idElement))
            return new Result<string?, string>.Error("Upload response missing item ID.");
        string? itemId = idElement.GetString();
        if (itemId is null)
            return new Result<string?, string>.Error("Upload response missing item ID.");

        return new Result<string?, string>.Ok(itemId);
    }
}
