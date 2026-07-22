using AStar.Dev.FunctionalParadigm;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Defines a single scrape operation that can be run against an already-configured Playwright page.
/// </summary>
public interface IScrapeAction
{
    /// <summary>
    ///     Gets the human-readable name shown in confirmation prompts and status text.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Runs the scrape against the supplied, already-configured page.
    /// </summary>
    /// <param name="page">The Playwright page to scrape.</param>
    /// <param name="progress">Receives human-readable progress messages as the scrape advances.</param>
    /// <param name="token">A token used to observe cancellation of the scrape.</param>
    Task<Exceptional<Unit>> ExecuteAsync(IPage page, IProgress<string> progress, CancellationToken cancellationToken);
}
