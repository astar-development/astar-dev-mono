namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Handles file downloads with automatic retry on 429 Too Many Requests.
/// Uses exponential backoff respecting the Retry-After header when present.
/// A new HttpClient is obtained from IHttpClientFactory per download call so the
/// factory can rotate and dispose handlers freely without this class holding stale references.
/// </summary>
public sealed class HttpDownloader(IHttpClientFactory httpClientFactory) : IHttpDownloader
{
    private const string UserAgent         = "AStar.Dev.OneDrive.Sync/1.0";
    private const int    MaxRetries        = 5;
    private const double BaseDelaySeconds  = 2.0;
    private const double MaxDelaySeconds   = 120.0;

    /// <summary>
    /// Downloads the file at <paramref name="url"/> to <paramref name="localPath"/>.
    /// Automatically retries on 429 with exponential backoff.
    /// Preserves the remote last-modified timestamp on the local file.
    /// </summary>
    /// <inheritdoc />
    public async Task DownloadAsync(string url, string localPath, DateTimeOffset remoteModified, IProgress<long>? progress = null, CancellationToken ct = default)
    {
        using var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Add("User-Agent", UserAgent);

        int attempt = 0;

        while(true)
        {
            ct.ThrowIfCancellationRequested();

            attempt++;
            HttpResponseMessage? response = null;

            try
            {
                response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);

                if(response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    if(attempt > MaxRetries)
                    {
                        throw new HttpRequestException($"Rate limited after {MaxRetries} retries.");
                    }

                    var delay = GetRetryDelay(response, attempt);
                    Serilog.Log.Warning("[HttpDownloader] 429 received, waiting {Delay:F1}s (attempt {Attempt}/{Max})", delay.TotalSeconds, attempt, MaxRetries);

                    response.Dispose();
                    await Task.Delay(delay, ct);
                    continue;
                }

                _ = response.EnsureSuccessStatusCode();

                string? dir = Path.GetDirectoryName(localPath);
                if(!string.IsNullOrEmpty(dir))
                    _ = Directory.CreateDirectory(dir);

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                await using var file = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);

                byte[] buffer    = new byte[81920];
                long written  = 0;
                int  read;

                while((read = await stream.ReadAsync(buffer, ct)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), ct);
                    written += read;
                    progress?.Report(written);
                }

                PreserveRemoteTimestamp(localPath, remoteModified);

                return;
            }
            catch(HttpRequestException) when(attempt <= MaxRetries)
            {
                var delay = GetBackoffDelay(attempt);
                Serilog.Log.Warning("[HttpDownloader] Network error, retrying in {Delay:F1}s (attempt {Attempt}/{Max})", delay.TotalSeconds, attempt, MaxRetries);
                await Task.Delay(delay, ct);
            }
            finally
            {
                response?.Dispose();
            }
        }
    }

    private static void PreserveRemoteTimestamp(string localPath, DateTimeOffset remoteModified) => File.SetLastWriteTimeUtc(localPath, remoteModified.UtcDateTime);

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if(response.Headers.RetryAfter?.Delta is { } delta)
            return delta + AddAdditionalSecondBackoff();

        if (response.Headers.RetryAfter?.Date is not { } date) return GetBackoffDelay(attempt);

        var wait = date - DateTimeOffset.UtcNow;
        if(wait > TimeSpan.Zero)
            return wait + AddAdditionalSecondBackoff();

        return GetBackoffDelay(attempt);
    }

    private static TimeSpan AddAdditionalSecondBackoff() => TimeSpan.FromSeconds(1);

    private static TimeSpan GetBackoffDelay(int attempt)
    {
        double seconds = CalculateExponentialBackoff(attempt);

        double jitter = seconds * 0.2 * Random.Shared.NextDouble();
        return TimeSpan.FromSeconds(seconds + jitter);
    }

    private static double CalculateExponentialBackoff(int attempt)
            => Math.Min(BaseDelaySeconds * Math.Pow(2, attempt - 1), MaxDelaySeconds);

}
