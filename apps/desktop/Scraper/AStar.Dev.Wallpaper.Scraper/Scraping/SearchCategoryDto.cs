namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Scrape progress for a single search category, as observed while visiting one of its pages.
/// </summary>
/// <param name="Name">The category's display name.</param>
/// <param name="IsFamous">Whether this category is considered "famous".</param>
/// <param name="IsInternet">Whether this category is considered an "internet" model.</param>
/// <param name="TotalPages">The total number of pages found for this category.</param>
/// <param name="LastKnownImageCount">The most recently observed wallpaper count for this category.</param>
/// <param name="LastPageVisited">The page number just visited.</param>
public sealed record SearchCategoryDto(string Name, bool IsFamous, bool IsInternet, int TotalPages, int LastKnownImageCount, int LastPageVisited);
