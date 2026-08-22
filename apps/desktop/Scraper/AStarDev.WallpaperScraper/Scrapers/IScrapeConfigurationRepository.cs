using AStar.Dev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.Scrapers;

/// <summary>
///  Represents a repository for scrape configuration settings repository.
/// </summary>
public interface IScrapeConfigurationRepository
{
    /// <summary>
    ///  Retrieves the scrape configuration settings.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the scrape configuration settings.</returns>
    Task<Exceptional<ScrapeConfiguration>> GetScrapeConfigurationAsync();
}
