namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
/// Directories used for scraping, including root directory, base save directory, and any subdirectories. This allows for flexible organization of scraped data and easy configuration of save paths.
/// </summary>
public class ScrapeDirectories : AuditableEntity
{
    /// <summary>
    /// The internal primary key for the ScrapeDirectories table.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The foreign key back to the parent scrape configuration.
    /// </summary>
    public int ScrapeConfigurationEntityId { get; set; }

    /// <summary>
    /// Navigation property to the parent scrape configuration.
    /// </summary>
    public ScrapeConfigurationEntity? ScrapeConfigurationEntity { get; set; }

    /// <summary>
    /// The root directory for all scraping activities. This is the base path under which all scraped data will be organized. It can be set to a specific location on the file system where the user wants to store the scraped images and related data. The base save directory and any subdirectories will be created under this root directory.
    /// </summary>
    public string RootDirectory { get; set; } = string.Empty;

    /// <summary>
    /// The base save directory is a subdirectory under the root directory where the scraped images will be saved. This allows for better organization of the scraped data, as users can specify different base save directories for different scraping sessions or categories. For example, if the root directory is set to "C:\ScrapedData", the base save directory could be "Wallpapers" or "SearchResults", resulting in a full path like "C:\ScrapedData\Wallpapers". This structure helps keep the scraped data organized and easily accessible.
    /// </summary>
    public string BaseSaveDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Subdirectories are additional folders that can be created under the base save directory to further organize the scraped data. For example, if the base save directory is "C:\ScrapedData\Wallpapers", subdirectories could be created for different categories or search terms, such as "Nature", "Cityscapes", or "Abstract". This allows users to easily navigate and manage their scraped images by categorizing them into relevant folders. The subdirectory name can be dynamically generated based on the search criteria or category being scraped, providing a flexible and organized way to store the scraped data.
    /// </summary>
    public string BaseDirectory { get; set; } = string.Empty;

    /// <summary>
    /// The base directory for famous wallpapers is a specific subdirectory under the root directory where wallpapers that are categorized as "famous" will be saved. This allows for a clear distinction between regular scraped wallpapers and those that are considered famous or highly popular. For example, if the root directory is "C:\ScrapedData", the base directory for famous wallpapers could be "FamousWallpapers", resulting in a full path like "C:\ScrapedData\FamousWallpapers". This helps users easily identify and access the wallpapers that are categorized as famous, keeping them organized separately from other scraped data.
    /// </summary>
    public string BaseDirectoryFamous { get; set; } = string.Empty;

    /// <summary>
    /// The subdirectory name is an additional folder that can be created under the base save directory to further organize the scraped data. This allows users to create specific folders for different search criteria, categories, or sessions. For example, if the base save directory is "C:\ScrapedData\Wallpapers", a subdirectory could be created for a specific search term like "Nature", resulting in a full path like "C:\ScrapedData\Wallpapers\Nature". This structure helps keep the scraped data organized and easily accessible based on the user's preferences and the context of the scraping session.
    /// </summary>
    public string SubDirectoryName { get; set; } = string.Empty;
}

