using System.IO.Abstractions;
using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <inheritdoc/>
/// <param name="fileSystem">The file system the image is written to.</param>
/// <param name="timeProvider">The time source used to wait before each download.</param>
/// <param name="pacing">The range of the random wait applied before each download.</param>
/// <param name="timeouts">The time an image body is allowed to take to arrive.</param>
public sealed class ImageDownloader(IFileSystem fileSystem, TimeProvider timeProvider, DownloadPacing pacing, ScrapeTimeouts timeouts) : IImageDownloader
{
    /// <summary>How often the user is told how much of a still-downloading image body has arrived. A download that finishes sooner reports nothing.</summary>
    public static readonly TimeSpan ProgressEvery = TimeSpan.FromSeconds(10);

    private const string PartialFileExtension = ".part";
    private const int BufferSize = 81920;

    /// <inheritdoc/>
    public Task<string> DownloadAsync(WallpaperFileRequest request, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken)
        => RequestTimeouts.RunAsync(() => DownloadCoreAsync(request, progress, client, cancellationToken), cancellationToken);

    private async Task<string> DownloadCoreAsync(WallpaperFileRequest request, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken)
    {
        await Task.Delay(pacing.NextDelay(), timeProvider, cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.Wallpaper.Path);

        using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        progress.Report($"Downloading image for wallpaper {request.Wallpaper.Id} from {request.Wallpaper.Path}");
        using Stream downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        _ = fileSystem.Directory.CreateDirectory(request.Directory);
        var savedPath = fileSystem.Path.Combine(request.Directory, request.FileName.Value);
        var partialPath = $"{savedPath}{PartialFileExtension}";
        try
        {
            using var bodyTimeout = new CancellationTokenSource(timeouts.ImageBody, timeProvider);
            using var bodyToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, bodyTimeout.Token);
            using (var fileStream = fileSystem.FileStream.New(partialPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await CopyReportingProgressAsync(downloadStream, fileStream, request.Wallpaper, progress, bodyToken.Token);
            }

            fileSystem.File.Move(partialPath, savedPath, overwrite: true);
        }
        finally
        {
            // Only reached with the partial file still present when the download failed or was cancelled: never leave a truncated image in the folder.
            if (fileSystem.File.Exists(partialPath)) fileSystem.File.Delete(partialPath);
        }

        return savedPath;
    }

    private async Task CopyReportingProgressAsync(Stream source, Stream destination, Data wallpaper, IProgress<string> progress, CancellationToken cancellationToken)
    {
        long received = 0;
        using var heartbeatStop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var heartbeat = ReportProgressPeriodicallyAsync(() => Interlocked.Read(ref received), wallpaper, progress, heartbeatStop.Token);
        try
        {
            var buffer = new byte[BufferSize];
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                _ = Interlocked.Add(ref received, read);
            }
        }
        finally
        {
            await heartbeatStop.CancelAsync();
            await heartbeat;
        }
    }

    private async Task ReportProgressPeriodicallyAsync(Func<long> bytesReceived, Data wallpaper, IProgress<string> progress, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(ProgressEvery, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var total = wallpaper.FileSize > 0 ? $" of {wallpaper.FileSize / 1024} KB" : string.Empty;
                progress.Report($"Still downloading wallpaper {wallpaper.Id} - {bytesReceived() / 1024} KB{total} received so far.");
            }
        }
        catch (OperationCanceledException)
        {
            // The body finished or the download was abandoned: the heartbeat simply ends.
        }
    }
}
