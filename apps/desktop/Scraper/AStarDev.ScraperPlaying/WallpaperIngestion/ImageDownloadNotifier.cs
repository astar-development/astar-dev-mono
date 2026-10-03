namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public sealed class ImageDownloadNotifier : IImageDownloadNotifier
{
    /// <inheritdoc/>
    public event EventHandler<WallpaperDownloadDetails>? ImageDownloaded;

    /// <inheritdoc/>
    public void NotifyImageDownloaded(WallpaperDownloadDetails details) => ImageDownloaded?.Invoke(this, details);
}
