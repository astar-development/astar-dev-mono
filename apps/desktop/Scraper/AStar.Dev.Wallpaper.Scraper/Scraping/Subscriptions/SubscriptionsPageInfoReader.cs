using System.Globalization;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping.Subscriptions;

/// <inheritdoc cref="ISubscriptionsPageInfoReader" />
public sealed class SubscriptionsPageInfoReader : ISubscriptionsPageInfoReader
{
    private const int ImagesPerPage = 24;

    /// <inheritdoc />
    public async Task<SubscriptionsPageInfo> ReadAsync(IPage page, CancellationToken cancellationToken)
    {
        var header = page.GetByText("New Subscription Wallpapers", new PageGetByTextOptions { Exact = false, });
        string? headerText = await header.TextContentAsync();
        int firstSpaceIndex = headerText!.IndexOf(' ');
        string countText = headerText.Replace(",", string.Empty)[..firstSpaceIndex];
        decimal imageCount = decimal.Parse(countText, CultureInfo.InvariantCulture);
        int pageCount = (int)Math.Ceiling(imageCount / ImagesPerPage);

        return new SubscriptionsPageInfo(pageCount, (int)imageCount);
    }
}
