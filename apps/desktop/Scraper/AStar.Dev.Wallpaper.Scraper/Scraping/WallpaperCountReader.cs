using AStar.Dev.Utilities;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Reads the total wallpaper count reported for the current search results page.
/// </summary>
public sealed class WallpaperCountReader : IWallpaperCountReader
{
    /// <inheritdoc />
    public async Task<int> ReadAsync(IPage page, CancellationToken cancellationToken)
    {
        var header = page.GetByText("Wallpapers found for", new PageGetByTextOptions { Exact = false, });
        var headerText = await header.AllTextContentsAsync();

        return headerText[0]?.Replace(",", string.Empty).Split(" ").FirstOrDefault()?.ToIntSafe() ?? 0;
    }
}
