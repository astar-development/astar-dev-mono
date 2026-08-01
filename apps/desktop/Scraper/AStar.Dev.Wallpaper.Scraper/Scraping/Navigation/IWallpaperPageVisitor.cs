using AStar.Dev.Wallpaper.Scraper.Scraping.Context;
namespace AStar.Dev.Wallpaper.Scraper.Scraping.Navigation;

/// <summary>
///     Visits a single wallpaper detail page and, unless it has already been downloaded or fails to load,
///     downloads, saves, and records it.
/// </summary>
public interface IWallpaperPageVisitor
{
    /// <summary>
    ///     Visits the wallpaper page at <paramref name="href" />, downloading and recording it unless it has
    ///     already been downloaded or the page fails to load.
    /// </summary>
    /// <param name="context">The scrape state shared across every stage of the current scrape.</param>
    /// <param name="href">The wallpaper detail page URL to visit.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the scrape.</param>
    Task VisitAsync(CategoryScrapeContext context, string href, CancellationToken cancellationToken);
}
