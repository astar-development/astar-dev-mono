namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>Which of the scrapes a run includes.</summary>
/// <param name="HotWallpapers">Whether the hot wallpapers scrape runs.</param>
/// <param name="TopWallpapers">Whether the top wallpapers scrape runs.</param>
/// <param name="Categories">Whether the search category scrapes run.</param>
public readonly record struct ScrapeSelection(bool HotWallpapers, bool TopWallpapers, bool Categories)
{
    /// <summary>Gets the selection that runs every scrape.</summary>
    public static ScrapeSelection All { get; } = new(true, true, true);
}
