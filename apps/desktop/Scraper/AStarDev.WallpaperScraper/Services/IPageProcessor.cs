using AStar.Dev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.Services;

/// <summary>
///  Represents a service that processes web pages, potentially using Playwright for browser automation.
/// </summary>
public interface IPageProcessor
{
    /// <summary>
    /// Processes a web page given its URL, reporting progress through the provided <see cref="IProgress{T}"/> instance.
    /// The method is asynchronous and can be cancelled via the provided <see cref="CancellationToken"/>.
    /// </summary>
    /// <param name="progress">The progress reporter to report status messages.</param>
    /// <param name="pageUrl">The URL of the page to process.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the result of the page processing.</returns>
    Task<Exceptional<PageResult>> ProcessPageAsync(IProgress<string> progress, Uri pageUrl, CancellationToken cancellationToken);
}
