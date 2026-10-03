using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>The validated scrape directories of a scrape configuration.</summary>
/// <param name="RootDirectory">The root directory for scraping.</param>
/// <param name="RootDirectoryFamous">The base directory for famous scraped content.</param>
/// <param name="SubDirectoryName">The name of the subdirectory for scraped content.</param>
public sealed record DirectorySettings(string RootDirectory, string RootDirectoryFamous, string SubDirectoryName) : IScrapeConfigurationSectionEdit
{
    /// <summary>Copies the scrape directories from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static DirectorySettings From(ScrapeConfigurationEntity entity) => new(
        entity.ScrapeDirectories.RootDirectory,
        entity.ScrapeDirectories.RootDirectoryFamous,
        entity.ScrapeDirectories.SubDirectoryName);

    /// <inheritdoc/>
    public void ApplyTo(ScrapeConfigurationEntity entity, DateTimeOffset now)
    {
        entity.ScrapeDirectories.RootDirectory = RootDirectory;
        entity.ScrapeDirectories.RootDirectoryFamous = RootDirectoryFamous;
        entity.ScrapeDirectories.SubDirectoryName = SubDirectoryName;
    }
}
