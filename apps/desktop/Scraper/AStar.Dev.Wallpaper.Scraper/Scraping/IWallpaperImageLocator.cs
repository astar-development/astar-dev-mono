using AStar.Dev.FunctionalParadigm;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Locates the full-size wallpaper image URL on a wallpaper detail page.
/// </summary>
public interface IWallpaperImageLocator
{
    /// <summary>
    ///     Reads the wallpaper image's source URL from an already-navigated wallpaper detail page.
    /// </summary>
    /// <param name="page">The Playwright page, already navigated to the wallpaper detail page.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the read.</param>
    /// <returns><see cref="Option{T}.None" /> when no usable source URL is present.</returns>
    Task<Option<string>> LocateAsync(IPage page, CancellationToken cancellationToken);
}
