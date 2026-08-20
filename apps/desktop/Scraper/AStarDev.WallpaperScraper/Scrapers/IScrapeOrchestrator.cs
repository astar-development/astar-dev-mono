using AStar.Dev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.Scrapers;

/// <summary>
/// Defines an interface for orchestrating wallpaper scraping operations, providing methods to scrape different categories of wallpapers asynchronously.
/// </summary>
public interface IScrapeOrchestrator
{
    /// <summary>
    /// Scrapes search categories asynchronously.
    /// </summary>
    /// <param name="progress">Receives real-time status messages as the scrape proceeds.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing an Exceptional result with UnitFp.</returns>
    Task<Exceptional<UnitFp>> ScrapeSearchCategoriesAsync(IProgress<string> progress, CancellationToken cancellationToken);

    /// <summary>
    /// Scrapes top wallpapers asynchronously.
    /// </summary>
    /// <param name="progress">Receives real-time status messages as the scrape proceeds.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing an Exceptional result with UnitFp.</returns>
    Task<Exceptional<UnitFp>> ScrapeTopAsync(IProgress<string> progress, CancellationToken cancellationToken);

    /// <summary>
    /// Scrapes subscribed wallpapers asynchronously.
    /// </summary>
    /// <param name="progress">Receives real-time status messages as the scrape proceeds.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing an Exceptional result with UnitFp.</returns>
    Task<Exceptional<UnitFp>> ScrapeSubscribedAsync(IProgress<string> progress, CancellationToken cancellationToken);

    /// <summary>
    /// Scrapes all wallpapers asynchronously.
    /// </summary>
    /// <param name="progress">Receives real-time status messages as the scrape proceeds.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing an Exceptional result with UnitFp.</returns>
    Task<Exceptional<UnitFp>> ScrapeAllAsync(IProgress<string> progress, CancellationToken cancellationToken);
}
