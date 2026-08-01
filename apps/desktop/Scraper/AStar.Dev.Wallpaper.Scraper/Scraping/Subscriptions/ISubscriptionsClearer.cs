using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping.Subscriptions;

/// <summary>
///     Clicks "Clear All Subscriptions" on the target site once a Subscriptions run has fully caught up.
/// </summary>
public interface ISubscriptionsClearer
{
    /// <summary>Clicks "Clear All Subscriptions" on the currently loaded Subscriptions page.</summary>
    /// <param name="page">The already-navigated Subscriptions page.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the click.</param>
    Task ClearAllAsync(IPage page, CancellationToken cancellationToken);
}
