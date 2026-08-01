using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping.TopWallpapers;

/// <summary>
///     Reads the total page count from an already-navigated Top Wallpapers page.
/// </summary>
public interface ITopWallpapersPageInfoReader
{
    /// <summary>Reads the "Page X / Y" header on an already-navigated Top Wallpapers page and returns Y.</summary>
    /// <param name="page">The already-navigated Top Wallpapers page.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the read.</param>
    Task<int> ReadPageCountAsync(IPage page, CancellationToken cancellationToken);
}
