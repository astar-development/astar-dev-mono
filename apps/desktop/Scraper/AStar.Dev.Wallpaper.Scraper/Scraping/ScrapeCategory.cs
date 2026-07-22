namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Represents a single search category and the URL used to list its wallpapers.
/// </summary>
/// <param name="Name">The category's display name.</param>
/// <param name="SearchUrl">The fully-built search URL for this category, before any page number is appended.</param>
/// <param name="IsFamous">Whether this category is considered "famous" and should be scraped before non-famous categories.</param>
/// <param name="IsInternet">Whether this category is considered an "internet" model and should be scraped after famous categories but before the rest.</param>
public sealed record ScrapeCategory(string Name, string SearchUrl, bool IsFamous, bool IsInternet);
