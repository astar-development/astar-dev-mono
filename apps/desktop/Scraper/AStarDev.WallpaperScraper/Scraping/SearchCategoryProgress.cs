namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>The scrape progress of an existing search category. Shown read-only and never edited.</summary>
/// <param name="LastKnownImageCount">The number of images observed as of the last scrape.</param>
/// <param name="LastPageVisited">The last page visited.</param>
/// <param name="TotalPages">The total number of pages available.</param>
public sealed record SearchCategoryProgress(int LastKnownImageCount, int LastPageVisited, int TotalPages);
