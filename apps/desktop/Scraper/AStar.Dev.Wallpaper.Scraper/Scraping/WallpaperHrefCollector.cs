using AStar.Dev.Wallpaper.Scraper.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Collects wallpaper detail-page links from a search results page.
/// </summary>
public sealed class WallpaperHrefCollector(IOptions<ScrapeConfiguration> scrapeConfiguration) : IWallpaperHrefCollector
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> CollectAsync(IPage page, CancellationToken cancellationToken)
    {
        var previews = await page.GetByRole(AriaRole.Link).AllAsync().ConfigureAwait(false);
        string baseUrl = scrapeConfiguration.Value.SearchConfiguration.BaseUrl.ToString().TrimEnd('/');
        string?[] hrefs = await Task.WhenAll(previews.Select(preview => preview.GetAttributeAsync("href"))).ConfigureAwait(false);

        return hrefs
            .Where(href => href is not null && href.StartsWith($"{baseUrl}/w/", StringComparison.InvariantCultureIgnoreCase))
            .Select(href => href!)
            .ToList();
    }
}
