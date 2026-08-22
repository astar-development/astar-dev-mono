namespace AStarDev.WallpaperScraper.Scrapers;

/// <summary>
/// Represents the configuration settings for scraping wallpapers from various sources.
/// </summary>
/// <param name="SearchCategoriesUrl">The base URL for the scraping of the Search Categories</param>
/// <param name="TopWallpapersUrl">The base URL for the scraping of the Top wallpapers</param>
/// <param name="SubscribedUrl">The base URL for the scraping of the Subscribed wallpapers</param>
public record ScrapeConfiguration(Uri SearchCategoriesUrl, Uri TopWallpapersUrl, Uri SubscribedUrl);
