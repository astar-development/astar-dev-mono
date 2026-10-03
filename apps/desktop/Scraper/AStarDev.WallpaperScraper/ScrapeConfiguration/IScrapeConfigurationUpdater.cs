using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>Persists edits made to a scrape configuration in the configuration editor.</summary>
public interface IScrapeConfigurationUpdater
{
    /// <summary>Applies the section edits to the specified scrape configuration and saves them in a single transaction.</summary>
    /// <param name="id">The identifier of the scrape configuration to change.</param>
    /// <param name="edits">The validated section edits to apply.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Some when saved, None when no scrape configuration has the identifier.</returns>
    Task<Exceptional<Option<Unit>>> SaveAsync(ScrapeConfigurationId id, IReadOnlyList<IScrapeConfigurationSectionEdit> edits, CancellationToken cancellationToken);
}
