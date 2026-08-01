namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     The page count and image count read from an already-navigated Subscriptions page header.
/// </summary>
/// <param name="PageCount">The total number of Subscriptions pages currently available.</param>
/// <param name="ImageCount">The total number of new subscription wallpapers currently available.</param>
public sealed record SubscriptionsPageInfo(int PageCount, int ImageCount);
