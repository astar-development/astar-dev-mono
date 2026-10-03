namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>Identifies one page of Wallhaven search results to fetch.</summary>
/// <param name="LogLabel">A label describing the search, used in the progress message.</param>
/// <param name="PageUrl">The URL of the page.</param>
/// <param name="Page">The page number, used in the progress message.</param>
public sealed record PageFetchRequest(string LogLabel, Uri PageUrl, int Page);
