namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>
/// Represents a scrape directories entity in the database.
/// </summary>
/// <param name="Id">The unique identifier for the scrape directories entity.</param>
/// <param name="ScrapeConfigurationEntityId">The unique identifier for the associated scrape configuration entity.</param>
/// <param name="RootDirectory">The root directory for scraping.</param>
/// <param name="BaseSaveDirectory">The base save directory for scraped content.</param>
/// <param name="BaseDirectory">The base directory for scraped content.</param>
/// <param name="BaseDirectoryFamous">The base directory for famous scraped content.</param>
/// <param name="SubDirectoryName">The name of the subdirectory for scraped content.</param>
public record ScrapeDirectoriesEntity(ScrapeDirectoriesId Id, ScrapeConfigurationId ScrapeConfigurationEntityId, string RootDirectory, string BaseSaveDirectory, string BaseDirectory, string BaseDirectoryFamous, string SubDirectoryName);
