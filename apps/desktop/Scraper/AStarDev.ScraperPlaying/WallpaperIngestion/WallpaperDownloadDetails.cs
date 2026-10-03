namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>The details of a wallpaper image that has just been written to disk, published via <see cref="IImageDownloadNotifier"/> so interested parties (such as a live preview) can react.</summary>
/// <param name="FilePath">The full path the image was saved to.</param>
/// <param name="Info">The wallpaper's display details.</param>
public sealed record WallpaperDownloadDetails(string FilePath, WallpaperInfo Info);
