using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Reads the total wallpaper count reported for the current search results page.
/// </summary>
public interface IWallpaperCountReader
{
    /// <summary>
    ///     Reads the "Wallpapers found for" header on an already-navigated results page.
    /// </summary>
    /// <param name="page">The Playwright page, already navigated to the search results.</param>
    /// <param name="token">A token used to observe cancellation of the read.</param>
    Task<int> ReadAsync(IPage page, CancellationToken cancellationToken);
}
