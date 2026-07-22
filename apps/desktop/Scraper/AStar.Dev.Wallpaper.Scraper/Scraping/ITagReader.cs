using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Reads the tags shown on a wallpaper detail page.
/// </summary>
public interface ITagReader
{
    /// <summary>
    ///     Reads every tag's name and category from an already-navigated wallpaper detail page.
    /// </summary>
    /// <param name="page">The Playwright page, already navigated to the wallpaper detail page.</param>
    /// <param name="token">A token used to observe cancellation of the read.</param>
    Task<IReadOnlyList<TagData>> ReadAsync(IPage page, CancellationToken cancellationToken);
}
