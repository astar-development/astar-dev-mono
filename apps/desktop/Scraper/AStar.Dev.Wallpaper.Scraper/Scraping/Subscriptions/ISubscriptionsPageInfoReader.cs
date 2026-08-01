using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping.Subscriptions;

/// <summary>
///     Reads the new-wallpaper count and derived page count from an already-navigated Subscriptions page.
/// </summary>
public interface ISubscriptionsPageInfoReader
{
    /// <summary>Reads the "New Subscription Wallpapers" header on an already-navigated Subscriptions page.</summary>
    /// <param name="page">The already-navigated Subscriptions page.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the read.</param>
    Task<SubscriptionsPageInfo> ReadAsync(IPage page, CancellationToken cancellationToken);
}
