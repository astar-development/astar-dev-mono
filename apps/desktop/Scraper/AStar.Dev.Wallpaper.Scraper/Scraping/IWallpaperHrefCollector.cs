using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Collects wallpaper detail-page links from a search results page.
/// </summary>
public interface IWallpaperHrefCollector
{
    /// <summary>
    ///     Reads every image preview link on an already-navigated results page, keeping only the ones that
    ///     point at a wallpaper detail page.
    /// </summary>
    /// <param name="page">The Playwright page, already navigated to the search results.</param>
    /// <param name="token">A token used to observe cancellation of the read.</param>
    Task<IReadOnlyList<string>> CollectAsync(IPage page, CancellationToken cancellationToken);
}
