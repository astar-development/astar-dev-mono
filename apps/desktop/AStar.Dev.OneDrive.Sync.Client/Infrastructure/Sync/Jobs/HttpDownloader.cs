using System.IO.Abstractions;
using System.Reactive;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Http;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

/// <summary>
/// Handles file downloads with automatic retry on 429 Too Many Requests.
/// Uses exponential backoff respecting the Retry-After header when present.
/// Downloads are written to a temporary path (<c>.download</c> suffix) and moved
/// atomically to the final path on completion, preventing conflicts with file-system
/// indexers or antivirus scanners that may open the destination file during a long sync.
/// A new HttpClient is obtained from IHttpClientFactory per download call so the
/// factory can rotate and dispose handlers freely without this class holding stale references.
/// </summary>
public sealed class HttpDownloader(IHttpClientFactory httpClientFactory, IFileSystem fileSystem, ILogger<HttpDownloader> logger) : IHttpDownloader
{
    private const string UserAgent = "AStar.Dev.OneDrive.Sync/1.0";
    private const int MaxMoveRetries = 3;
    private static readonly TimeSpan MoveRetryDelay = TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    public async Task<Result<Unit, string>> DownloadAsync(string url, string localPath, DateTimeOffset remoteModified, IProgress<long>? progress = null, CancellationToken ct = default)
    {
        using var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Add("User-Agent", UserAgent);

        string tempPath = localPath + "." + Guid.NewGuid().ToString("N") + ".download";
        int attempt = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            attempt++;
            HttpResponseMessage? response = null;

            try
            {
                response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    if (attempt > HttpRetryPolicy.MaxRetries)
                        return new Result<Unit, string>.Error($"Rate limited after {HttpRetryPolicy.MaxRetries} retries.");

                    var delay = HttpRetryPolicy.GetRetryDelay(response, attempt);
                    OneDriveSyncClientMessages.DownloadThrottled(logger, delay.TotalSeconds, attempt, HttpRetryPolicy.MaxRetries);

                    response.Dispose();
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                    continue;
                }

                _ = response.EnsureSuccessStatusCode();

                EnsureDirectoryExists(localPath);

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                await WriteToFileAsync(stream, tempPath, progress, ct).ConfigureAwait(false);

                return await MoveWithRetryAsync(tempPath, localPath, ct)
                    .MatchAsync<Unit, string, Result<Unit, string>>(
                        _ => { PreserveRemoteTimestamp(localPath, remoteModified); return new Result<Unit, string>.Ok(Unit.Default); },
                        error => { TryDeleteTemp(tempPath); return new Result<Unit, string>.Error(error); }).ConfigureAwait(false);
            }
            catch (IOException) when (attempt <= HttpRetryPolicy.MaxRetries)
            {
                TryDeleteTemp(tempPath);
                var delay = HttpRetryPolicy.GetBackoffDelay(attempt);
                OneDriveSyncClientMessages.DownloadNetworkError(logger, delay.TotalSeconds, attempt, HttpRetryPolicy.MaxRetries);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                TryDeleteTemp(tempPath);
                return new Result<Unit, string>.Error($"IO error downloading '{localPath}': {ex.Message}");
            }
            catch (HttpRequestException) when (attempt <= HttpRetryPolicy.MaxRetries)
            {
                var delay = HttpRetryPolicy.GetBackoffDelay(attempt);
                OneDriveSyncClientMessages.DownloadNetworkError(logger, delay.TotalSeconds, attempt, HttpRetryPolicy.MaxRetries);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                OneDriveSyncClientMessages.DownloadCancelledDuringBackoff(logger, url, attempt, HttpRetryPolicy.MaxRetries);
                throw;
            }
            finally
            {
                response?.Dispose();
            }
        }
    }

    private async Task<Result<Unit, string>> MoveWithRetryAsync(string tempPath, string localPath, CancellationToken ct)
    {
        IOException? lastError = null;

        for (int attempt = 1; attempt <= MaxMoveRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                fileSystem.File.Move(tempPath, localPath, overwrite: true);
                return new Result<Unit, string>.Ok(Unit.Default);
            }
            catch (IOException ex)
            {
                lastError = ex;

                if (attempt < MaxMoveRetries)
                {
                    OneDriveSyncClientMessages.DownloadMoveRetrying(logger, localPath, attempt, MaxMoveRetries);
                    await Task.Delay(MoveRetryDelay, ct).ConfigureAwait(false);
                }
            }
        }

        OneDriveSyncClientMessages.DownloadMoveExhausted(logger, localPath, MaxMoveRetries, lastError?.Message ?? string.Empty);
        return new Result<Unit, string>.Error($"Could not move downloaded file to '{localPath}' after {MaxMoveRetries} attempts: {lastError?.Message}");
    }

    private void TryDeleteTemp(string tempPath)
    {
        try
        {
            fileSystem.File.Delete(tempPath);
        }
        catch (Exception)
        {
            // Best-effort cleanup — do not mask the original error.
        }
    }

    private async Task WriteToFileAsync(Stream source, string localPath, IProgress<long>? progress, CancellationToken ct)
    {
        await using var file = fileSystem.FileStream.New(localPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);

        byte[] buffer = new byte[81920];
        long written = 0;
        int read;

        while ((read = await source.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            written += read;
            progress?.Report(written);
        }
    }

    private void EnsureDirectoryExists(string localPath)
    {
        string? dir = fileSystem.Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(dir))
            _ = fileSystem.Directory.CreateDirectory(dir);
    }

    private void PreserveRemoteTimestamp(string localPath, DateTimeOffset remoteModified) => fileSystem.File.SetLastWriteTimeUtc(localPath, remoteModified.UtcDateTime);
}
