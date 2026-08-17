namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
/// Represents the connection strings required for the scraper to connect to various databases or services. This class is essential for configuring the scraper's access to necessary resources, such as a SQLite database for storing scraped data or a logging service for recording the scraping process. The ConnectionStrings property allows for flexible configuration of different connection strings, enabling the scraper to adapt to various environments and requirements without hardcoding sensitive information directly into the codebase. Proper management of connection strings is crucial for ensuring secure and efficient operation of the scraper, as it may need to connect to multiple services or databases during its execution.
/// </summary>
public class ConnectionStrings : AuditableEntity
{
    /// <summary>
    /// The internal primary key for the ConnectionStrings table.
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
    /// The connection string for the SQLite database. This string contains the necessary information for the scraper to establish a connection to the SQLite database, such as the file path, credentials, and any additional parameters required for authentication and communication. The Sqlite connection string is crucial for enabling the scraper to store and retrieve data efficiently, allowing it to manage the scraped data effectively and maintain a persistent record of its operations. Proper configuration of the SQLite connection string ensures that the scraper can access the database securely and perform necessary read/write operations without issues.
    /// </summary>
    public string Sqlite { get; set; } = string.Empty;
}

