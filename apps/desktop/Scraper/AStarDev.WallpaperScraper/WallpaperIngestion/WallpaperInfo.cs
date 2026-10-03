using System.Globalization;

namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>The details of a wallpaper shown alongside its image.</summary>
/// <param name="Name">The saved file's actual name, including its extension and any person-name prefix.</param>
/// <param name="CategoryLabel">The search category the wallpaper came from, or "Top Wallpapers" when it did not come from a specific search category.</param>
/// <param name="FileSizeBytes">The wallpaper's file size, in bytes.</param>
/// <param name="Width">The wallpaper's width, in pixels.</param>
/// <param name="Height">The wallpaper's height, in pixels.</param>
/// <param name="Count">How far through its search the wallpaper's download is; the default when that is not known.</param>
public sealed record WallpaperInfo(string Name, string CategoryLabel, int FileSizeBytes, int Width, int Height, SearchCount Count = default)
{
    /// <summary>Gets the category label followed by the current and total counts, e.g. "Top Wallpapers (3 of 1,124)"; just the label when the total is not known.</summary>
    public string CategoryDescription => Count.Total > 0
        ? string.Create(CultureInfo.CurrentCulture, $"{CategoryLabel} ({Count.Current:N0} of {Count.Total:N0})")
        : CategoryLabel;
}
