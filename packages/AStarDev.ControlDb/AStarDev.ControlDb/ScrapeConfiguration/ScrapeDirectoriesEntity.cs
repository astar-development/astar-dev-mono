namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>Represents a scrape directories entity in the database.</summary>
/// <param name="Id">The unique identifier for the scrape directories entity.</param>
/// <param name="ScrapeConfigurationEntityId">The unique identifier for the associated scrape configuration entity.</param>
/// <param name="RootDirectory">The root directory for scraping.</param>
/// <param name="RootDirectoryFamous">The base directory for famous scraped content.</param>
/// <param name="SubDirectoryName">The name of the subdirectory for scraped content.</param>
public record ScrapeDirectoriesEntity(ScrapeDirectoriesId Id, ScrapeConfigurationId ScrapeConfigurationEntityId, string RootDirectory, string RootDirectoryFamous, string SubDirectoryName)
{
    /// <summary>The root directory for scraping.</summary>
    public string RootDirectory { get; set; } = RootDirectory;

    /// <summary>The base directory for famous scraped content.</summary>
    public string RootDirectoryFamous { get; set; } = RootDirectoryFamous;

    /// <summary>The name of the subdirectory for scraped content.</summary>
    public string SubDirectoryName { get; set; } = SubDirectoryName;
}
