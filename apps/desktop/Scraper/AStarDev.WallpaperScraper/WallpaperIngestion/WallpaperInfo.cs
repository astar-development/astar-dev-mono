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
    /// <summary>Gets the category label, indented by two tabs, followed by the image and page counts, e.g. "Cars\t\t(Image 147 of 2,000, page 6 of 27)"; the page details are left out when the page total is not known and just the label is given when the image total is not known.</summary>
    public string CategoryDescription => Count.Total > 0
        ? string.Create(CultureInfo.CurrentCulture, $"{CategoryLabel}\t\t(Image {Count.Current:N0} of {Count.Total:N0}{PageDetails})")
        : CategoryLabel;

    private string PageDetails => Count.TotalPages > 0
        ? string.Create(CultureInfo.CurrentCulture, $", page {Count.Page:N0} of {Count.TotalPages:N0}")
        : string.Empty;
}
