namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
/// Represents a search category with its associated metadata, including the category's unique identifier, name, last known image count, last page visited, and total pages available. This information is crucial for managing and tracking the scraping process for each category, allowing for efficient navigation through search results and ensuring that the scraper can resume from the last visited page in case of interruptions. The LastKnownImageCount helps in determining if there are new images to scrape since the last run, while LastPageVisited and TotalPages assist in navigating through paginated search results effectively.
/// </summary>
public class SearchCategories : AuditableEntity
{
    /// <summary>
    /// The foreign key back to the parent search configuration.
    /// </summary>
    public int SearchConfigurationId { get; set; }

    /// <summary>
    /// Navigation property to the parent search configuration.
    /// </summary>
    public SearchConfiguration? SearchConfiguration { get; set; }

    /// <summary>
    /// The unique identifier for the search category. This ID is used to distinguish between different categories and is essential for tracking the scraping progress for each specific category. It allows the scraper to identify which category it is currently processing and to store relevant data accordingly.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the search category. This is a human-readable string that describes the category being scraped, such as "Nature", "City-scapes", or "Abstract". The name helps users understand the context of the category and can be used for organizing and labeling the scraped data. It also provides clarity when reviewing logs or reports related to the scraping process, making it easier to identify which categories were processed and how many images were scraped for each category.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The last known image count for the search category. This property keeps track of the number of images that were last scraped for this category. It is useful for determining if there are new images to scrape since the last run, allowing the scraper to focus on new content and avoid re-scraping already processed images. By comparing the current image count with the last known count, the scraper can efficiently manage its resources and prioritize categories that have new content available.
    /// </summary>
    public int LastKnownImageCount { get; set; }

    /// <summary>
    /// The last page visited for the search category. This property is crucial for managing the pagination of search results. It allows the scraper to resume from the last visited page in case of interruptions, ensuring that it does not miss any images or re-scrape already processed pages. By keeping track of the last page visited, the scraper can efficiently navigate through paginated search results and maintain a consistent scraping process across multiple runs.
    /// </summary>
    public int LastPageVisited { get; set; }

    /// <summary>
    /// The total number of pages available for the search category. This property provides insight into the scope of the scraping task for each category, allowing the scraper to plan its navigation through the search results effectively. Knowing the total pages helps in estimating the time required for scraping and in managing expectations regarding the volume of data that will be collected for each category. It also assists in determining when to stop scraping for a category, ensuring that the scraper does not attempt to access non-existent pages and handles pagination gracefully.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Indicates whether the search category should be included in the scraping process. This boolean property allows for selective scraping, enabling users to exclude certain categories from being processed based on specific criteria or preferences. By setting this property to false, the scraper can skip over categories that are not relevant or desired, optimizing the scraping process and focusing resources on categories that are of interest. This feature is particularly useful for managing large datasets and ensuring that the scraper operates efficiently by avoiding unnecessary processing of unwanted categories.
    /// </summary>
    public bool IncludeInSearch { get; set; } = true;
}

