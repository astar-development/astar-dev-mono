using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Decides which searches a scrape runs for a configuration and runs them in order.</summary>
public interface ISearchOrchestrator
{
    /// <summary>Runs the hot wallpapers, top wallpapers and configured category searches that <paramref name="selection"/> includes, in that order.</summary>
    /// <param name="configuration">The scrape configuration to search with.</param>
    /// <param name="selection">Which of the scrapes to run.</param>
    /// <param name="progress">The progress reporter to report search progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    Task RunSearchesAsync(ScrapeConfigurationEntity configuration, ScrapeSelection selection, IProgress<string> progress, CancellationToken cancellationToken);
}
