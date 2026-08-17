namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>The directories the scraper reads from and writes scraped data to.</summary>
public sealed class ScrapeDirectoriesEntity : AuditableEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the parent scrape configuration.</summary>
    public int ScrapeConfigurationEntityId { get; set; }

    /// <summary>Navigation property to the parent scrape configuration.</summary>
    public ScrapeConfigurationEntity? ScrapeConfigurationEntity { get; set; }

    /// <summary>The root directory under which all scraped data is organised.</summary>
    public string RootDirectory { get; set; } = string.Empty;

    /// <summary>The subdirectory, under the root directory, where scraped images are saved.</summary>
    public string BaseSaveDirectory { get; set; } = string.Empty;

    /// <summary>An additional subdirectory used to further organise scraped data.</summary>
    public string BaseDirectory { get; set; } = string.Empty;

    /// <summary>The subdirectory where wallpapers categorised as famous are saved.</summary>
    public string BaseDirectoryFamous { get; set; } = string.Empty;

    /// <summary>A subdirectory name generated from the search criteria or category.</summary>
    public string SubDirectoryName { get; set; } = string.Empty;
}
