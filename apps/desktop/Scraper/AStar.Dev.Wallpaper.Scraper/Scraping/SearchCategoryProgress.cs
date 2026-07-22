namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     The stored scrape progress for a single search category, as last persisted to the database.
/// </summary>
/// <param name="LastKnownImageCount">The most recently observed wallpaper count for this category.</param>
/// <param name="LastPageVisited">The last page number visited for this category.</param>
public sealed record SearchCategoryProgress(int LastKnownImageCount, int LastPageVisited);
