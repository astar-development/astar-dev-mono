using AStarDev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>Represents a service for importing scrape configuration settings.</summary>
public interface IScrapeConfigurationImporter
{
    /// <summary>Replaces the persisted scrape configuration aggregate.</summary>
    Task<Exceptional<Unit>> ImportScrapeConfigurationAsync(ScrapeConfigurationImportDocument document, CancellationToken cancellationToken = default);
}
