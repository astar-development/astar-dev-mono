using AStar.Dev.FunctionalParadigm;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Locates the full-size wallpaper image URL on a wallpaper detail page.
/// </summary>
public sealed class WallpaperImageLocator : IWallpaperImageLocator
{
    /// <inheritdoc />
    public async Task<Option<string>> LocateAsync(IPage page, CancellationToken cancellationToken)
    {
        string? imageUrl = await page.Locator("#wallpaper").GetAttributeAsync("src").ConfigureAwait(false);

        return (imageUrl ?? string.Empty).ToOption(url => !string.IsNullOrWhiteSpace(url));
    }
}
