namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>
/// Interface for publishing when a wallpaper's image has been saved to disk, so interested parties (such as a
/// live preview) can react without the download path needing to know about them.
/// </summary>
public interface IImageDownloadNotifier
{
    /// <summary>Raised after a wallpaper's image has been written to disk.</summary>
    event EventHandler<WallpaperDownloadDetails>? ImageDownloaded;

    /// <summary>Raises <see cref="ImageDownloaded"/> for the image described by <paramref name="details"/>.</summary>
    /// <param name="details">The details of the wallpaper image that was saved.</param>
    void NotifyImageDownloaded(WallpaperDownloadDetails details);
}
