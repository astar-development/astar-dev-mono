namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Represents a wallpaper image that has been written to disk.
/// </summary>
/// <param name="FullPath">The full path the image was written to.</param>
/// <param name="SizeBytes">The size of the written file, in bytes.</param>
public sealed record SavedWallpaperFile(string FullPath, long SizeBytes);
