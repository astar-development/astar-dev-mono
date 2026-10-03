using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Reads the root scrape configurations so one can be selected for editing.</summary>
public interface IScrapeConfigurationCatalogue
{
    /// <summary>Lists a summary of every root scrape configuration.</summary>
    Task<Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Finds the complete scrape configuration aggregate with the specified identifier.</summary>
    /// <param name="id">The identifier of the scrape configuration.</param>
    Task<Exceptional<Option<ScrapeConfigurationEntity>>> FindAsync(ScrapeConfigurationId id, CancellationToken cancellationToken = default);
}
