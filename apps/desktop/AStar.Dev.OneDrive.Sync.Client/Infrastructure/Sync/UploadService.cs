using System.Globalization;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

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
public sealed class UploadService(IHttpClientFactory httpClientFactory): IDisposable, IUploadService
{
    private const int ChunkSize10Mb = 10 * 1024 * 1024;

    private const int    MaxRetries       = 5;
    private const double BaseDelaySeconds = 2.0;
    private const double MaxDelaySeconds  = 120.0;

    private readonly HttpClient _http = httpClientFactory.CreateClient();

    /// <summary>
    /// Uploads a local file to OneDrive using a resumable upload session.
    /// Returns the uploaded DriveItem ID on success.
    /// </summary>
    public async Task<string> UploadAsync(GraphServiceClient client, string driveId, string parentFolderId, string localPath, string remotePath, IProgress<long>? progress = null, CancellationToken ct = default)
    {
        var fileInfo = new FileInfo(localPath);
        if(!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Local file not found: {localPath}");
        }

        Serilog.Log.Information("[UploadService] Starting upload: {Path} ({Size:F2} MB)", remotePath, fileInfo.Length / (1024.0 * 1024));

        string sessionUrl = await CreateSessionWithRetryAsync(client, driveId, parentFolderId, remotePath, fileInfo.LastWriteTimeUtc, ct);

        string itemId = await UploadChunksAsync(sessionUrl, localPath, fileInfo.Length, progress, ct);

        Serilog.Log.Information("[UploadService] Upload complete: {Path} → {ItemId}", remotePath, itemId);

        return itemId;
    }

    private static async Task<string> CreateSessionWithRetryAsync(GraphServiceClient client, string driveId, string parentFolderId, string remotePath, DateTime lastModified, CancellationToken ct)
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
            .PostAsync(requestBody, cancellationToken: ct);

        return session?.UploadUrl is null
            ? throw new InvalidOperationException("Graph API did not return an upload session URL.")
            : session.UploadUrl;
    }

    private async Task<string> UploadChunksAsync(string sessionUrl, string localPath, long totalBytes, IProgress<long>? progress, CancellationToken ct)
    {
        await using var file = File.OpenRead(localPath);
        byte[] buffer    = new byte[ChunkSize10Mb];
        long uploaded  = 0L;

        while(uploaded < totalBytes)
        {
            ct.ThrowIfCancellationRequested();

            int bytesToRead = (int)Math.Min(ChunkSize10Mb, totalBytes - uploaded);
            int bytesRead   = await file.ReadAsync(
                buffer.AsMemory(0, bytesToRead), ct);

            if(bytesRead == 0)
                break;

            long rangeEnd  = uploaded + bytesRead - 1;
            string? itemId    = await UploadChunkWithRetryAsync(sessionUrl, buffer.AsMemory(0, bytesRead), uploaded, rangeEnd, totalBytes, ct);

            uploaded += bytesRead;
            progress?.Report(uploaded);

            if(itemId is not null)
                return itemId;
        }

        throw new InvalidOperationException("Upload completed without receiving item ID from Graph API.");
    }

    private async Task<string?> UploadChunkWithRetryAsync(string sessionUrl, ReadOnlyMemory<byte> chunk, long rangeStart, long rangeEnd, long totalBytes, CancellationToken ct)
    {
        int attempt = 0;

        while(true)
        {
            attempt++;
            ct.ThrowIfCancellationRequested();

            try
            {
                using var content = new ByteArrayContent(chunk.ToArray());
                content.Headers.Add("Content-Range", $"bytes {rangeStart}-{rangeEnd}/{totalBytes}");
                content.Headers.Add("Content-Length", chunk.Length.ToString(CultureInfo.CurrentCulture));

                using var response = await _http.PutAsync(sessionUrl, content, ct);

                if(response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    if(attempt > MaxRetries)
                    {
                        throw new HttpRequestException($"Upload rate limited after {MaxRetries} retries.");
                    }

                    var delay = GetRetryDelay(response, attempt);
                    Serilog.Log.Warning("[UploadService] 429 on chunk {Start}-{End}, waiting {Delay:F1}s (attempt {A}/{Max})", rangeStart, rangeEnd, delay.TotalSeconds, attempt, MaxRetries);

                    await Task.Delay(delay, ct);
                    continue;
                }

                if(response.StatusCode == System.Net.HttpStatusCode.Accepted)
                    return null;

                if(response.StatusCode is System.Net.HttpStatusCode.Created or System.Net.HttpStatusCode.OK)
                    return await GetUploadedDocumentId(response, ct);

                _ = response.EnsureSuccessStatusCode();

                return null;
            }
            catch(HttpRequestException) when(attempt <= MaxRetries)
            {
                var delay = GetBackoffDelay(attempt);
                Serilog.Log.Warning("[UploadService] Network error on chunk {Start}-{End}, retrying in {Delay:F1}s (attempt {A}/{Max})", rangeStart, rangeEnd, delay.TotalSeconds, attempt, MaxRetries);

                await Task.Delay(delay, ct);
            }
        }
    }

    private static async Task<string?> GetUploadedDocumentId(HttpResponseMessage response, CancellationToken ct)
    {
        string json = await response.Content.ReadAsStringAsync(ct);
        using var doc = System.Text.Json.JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("id")
            .GetString()
            ?? throw new InvalidOperationException("Upload response missing item ID.");
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if(response.Headers.RetryAfter?.Delta is { } delta)
            return delta + TimeSpan.FromSeconds(1);

        if(response.Headers.RetryAfter?.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            if(wait > TimeSpan.Zero)
                return wait + TimeSpan.FromSeconds(1);
        }

        return GetBackoffDelay(attempt);
    }

    private static TimeSpan GetBackoffDelay(int attempt)
    {
        double seconds = Math.Min(BaseDelaySeconds * Math.Pow(2, attempt - 1), MaxDelaySeconds);
        double jitter = seconds * 0.2 * Random.Shared.NextDouble();

        return TimeSpan.FromSeconds(seconds + jitter);
    }

    public void Dispose() => _http.Dispose();
}
