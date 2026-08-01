using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping.Subscriptions;

/// <inheritdoc cref="ISubscriptionsClearer" />
public sealed class SubscriptionsClearer : ISubscriptionsClearer
{
    /// <inheritdoc />
    public async Task ClearAllAsync(IPage page, CancellationToken cancellationToken) =>
        await page.Locator("div")
            .Filter(new LocatorFilterOptions { HasText = " Clear All Subscriptions", })
            .GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = " Clear All Subscriptions", })
            .ClickAsync();
}
