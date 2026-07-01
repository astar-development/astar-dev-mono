namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
/// Represents the configuration details required for the scraping process, including connection strings, user configuration, search configuration, and scrape directories. This class serves as a central point for managing all the necessary settings and parameters that the scraper needs to operate effectively. The ConnectionStrings property contains the information required to connect to various databases or services, while the UserConfiguration property holds the authentication details for accessing user-specific content on the target website. The SearchConfiguration property defines the parameters for performing searches on the target website, such as search queries and filters. Finally, the ScrapeDirectories property specifies the directories where scraped data should be stored. Proper management of these configurations is crucial for ensuring that the scraper can operate efficiently and securely, allowing it to access necessary resources and perform its tasks without issues.
/// </summary>
public class ScrapeConfigurationEntity : AuditableEntity
{
    /// <summary>
    /// The unique identifier for the scrape configuration entity. This property serves as the primary key for the entity in the database, allowing for efficient retrieval and management of different scrape configurations. The Id is typically an auto-incrementing integer that uniquely distinguishes each configuration entry, enabling users to store and manage multiple configurations for different scraping scenarios or target websites. Proper management of the Id property is essential for maintaining the integrity of the database and ensuring that each configuration can be accessed and modified as needed without conflicts or ambiguity.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The connection strings required for the scraper to connect to various databases or services. This property is essential for configuring the scraper's access to necessary resources, such as a SQLite database for storing scraped data or a logging service for recording the scraping process. The ConnectionStrings property allows for flexible configuration of different connection strings, enabling the scraper to adapt to various environments and requirements without hardcoding sensitive information directly into the codebase. Proper management of connection strings is crucial for ensuring secure and efficient operation of the scraper, as it may need to connect to multiple services or databases during its execution.
    /// </summary>
    public ConnectionStrings ConnectionStrings { get; set; } = new();

    /// <summary>
    /// Represents user configuration details, including login email address, username, password, and session cookie. This information is essential for authenticating the scraper with the target website, allowing it to access user-specific content and perform actions that require authentication. The LoginEmailAddress and Username are used for identifying the user account, while the Password is necessary for logging in. The SessionCookie can be used to maintain an authenticated session without needing to log in repeatedly, improving the efficiency of the scraping process and reducing the likelihood of triggering anti-bot measures on the target website.
    /// </summary>
    public UserConfiguration UserConfiguration { get; set; } = new();

    /// <summary>
    /// Represents the search configuration details for the scraper, including search queries, filters, and other parameters that define how the scraper should perform searches on the target website. This property is crucial for guiding the scraper's behavior when it comes to finding and retrieving specific content from the website. The SearchConfiguration allows for flexible and dynamic configuration of search parameters, enabling the scraper to adapt to different search requirements and scenarios without needing to modify the codebase. Proper management of search configuration is essential for ensuring that the scraper can effectively find and retrieve the desired content while adhering to any constraints or limitations imposed by the target website.
    /// </summary>
    public SearchConfiguration SearchConfiguration { get; set; } = new();

    /// <summary>
    /// Represents the directories where scraped data should be stored. This property is essential for organizing and managing the output of the scraping process, allowing the scraper to save retrieved data in a structured and accessible manner. The ScrapeDirectories property can include paths for different types of data, such as images, metadata, or logs, enabling the scraper to maintain a clear separation of different data categories. Proper management of scrape directories is crucial for ensuring that the scraper can efficiently store and retrieve scraped data, as well as for maintaining an organized file system that facilitates easy access and analysis of the collected information.
    /// </summary>
    public ScrapeDirectories ScrapeDirectories { get; set; } = new();
}

