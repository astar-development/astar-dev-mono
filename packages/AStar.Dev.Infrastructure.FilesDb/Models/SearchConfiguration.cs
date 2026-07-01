namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
/// Represents the search configuration for the scraping process, including the base URL, API key, search categories, search string, top wallpapers, search string prefix and suffix, subscriptions, image pause duration, starting page numbers, and total pages for search results, subscriptions, and top wallpapers. This configuration is essential for guiding the scraper on how to navigate the target website, what criteria to use for searching and categorizing images, and how to manage pagination effectively. The properties in this class allow for flexible and dynamic configuration of the scraping process, enabling users to customize their scraping sessions according to their specific needs and preferences.
/// </summary>
public class SearchConfiguration : AuditableEntity
{
    /// <summary>
    /// The internal primary key for the SearchConfiguration table.
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
    /// The base URL of the target website to be scraped. This is the starting point for the scraper and is essential for constructing the full URLs for search queries, image pages, and other relevant endpoints. The base URL should be set to the main domain of the website being scraped, such as "https://example.com". It serves as the foundation for all subsequent navigation and scraping activities, allowing the scraper to access the necessary pages and resources to collect the desired data effectively.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The API key is a unique identifier used to authenticate requests to the target website's API, if applicable. This key is essential for accessing certain features or data that may be restricted to authenticated users or for making authorized requests to the website's API endpoints. The API key should be kept secure and should not be shared publicly, as it may grant access to sensitive information or allow unauthorized actions if misused. In the context of web scraping, the API key can help ensure that the scraper can access the necessary data while adhering to the website's usage policies and rate limits.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The search categories represent the different categories or topics that the scraper will target during the scraping process. Each category includes its unique identifier, name, last known image count, last page visited, and total pages available. This information is crucial for managing and tracking the scraping progress for each category, allowing the scraper to efficiently navigate through search results and resume from the last visited page in case of interruptions. The search categories help organize the scraping process and ensure that the scraper can focus on specific areas of interest while collecting relevant data effectively.
    /// </summary>
    public List<SearchCategories> SearchCategories { get; set; } = new();

    /// <summary>
    /// The search string is a specific query or keyword that the scraper will use to perform searches on the target website. This string can be customized to target specific types of content or to refine the search results based on user preferences. The search string is essential for guiding the scraper in finding relevant images and data that match the specified criteria, allowing for a more focused and efficient scraping process. By using a well-defined search string, users can ensure that the scraper collects data that is most relevant to their interests and needs.
    /// </summary>
    public string SearchString { get; set; } = string.Empty;

    /// <summary>
    /// The top wallpapers property represents a specific category or section on the target website that features the most popular or highly rated wallpapers. This property is essential for guiding the scraper to access and collect data from this particular section, allowing users to obtain high-quality and trending wallpapers. By targeting the top wallpapers, users can ensure that they are collecting content that is currently in demand and has a higher likelihood of being of interest to a wider audience. This can be particularly useful for users who want to stay updated with the latest trends in wallpaper designs and styles.
    /// </summary>
    public string TopWallpapers { get; set; } = string.Empty;

    /// <summary>
    /// The search string prefix and suffix are additional components that can be added to the search string to further refine and customize the search queries. The prefix can be used to specify certain criteria or filters that should be applied before the main search string, while the suffix can be used to add additional parameters or conditions after the main search string. These components allow for greater flexibility in constructing search queries, enabling users to target specific types of content or to exclude certain results based on their preferences. By using a combination of the search string, prefix, and suffix, users can create more complex and effective search queries that yield more relevant results during the scraping process.
    /// </summary>
    public string SearchStringPrefix { get; set; } = string.Empty;

    /// <summary>
    /// The search string suffix is an additional component that can be added to the search string to further refine and customize the search queries. It can be used to specify certain criteria or filters that should be applied after the main search string, allowing for greater flexibility in constructing search queries. By using a combination of the search string, prefix, and suffix, users can create more complex and effective search queries that yield more relevant results during the scraping process. The suffix can help target specific types of content or exclude certain results based on user preferences, enhancing the overall efficiency and relevance of the scraping activities.
    /// </summary>
    public string SearchStringSuffix { get; set; } = string.Empty;

    /// <summary>
    /// The subscriptions property represents a specific section or feature on the target website that allows users to subscribe to certain categories, topics, or content creators. This property is essential for guiding the scraper to access and collect data from this particular section, enabling users to obtain content that they have subscribed to or are interested in following. By targeting the subscriptions, users can ensure that they are collecting data that is relevant to their specific interests and preferences, allowing for a more personalized and engaging scraping experience. This can be particularly useful for users who want to stay updated with content from their favorite categories or creators on the target website.
    /// </summary>
    public string Subscriptions { get; set; } = string.Empty;

    /// <summary>
    /// The image pause duration in seconds is a configurable parameter that determines the amount of time the scraper will wait between processing individual images during the scraping process. This pause is essential for managing the scraper's behavior and ensuring that it does not overwhelm the target website with rapid requests, which could lead to rate limiting or blocking. By introducing a pause between image processing, users can help ensure that the scraper operates within acceptable limits and adheres to the website's usage policies, while also allowing for a more controlled and efficient scraping process. The duration of the pause can be adjusted based on the user's preferences and the target website's responsiveness, providing flexibility in managing the scraping activities effectively.
    /// </summary>
    public int ImagePauseInSeconds { get; set; }

    /// <summary>
    /// The starting page numbers and total pages for search results, subscriptions, and top wallpapers are crucial parameters for managing the pagination of the scraping process. These properties allow the scraper to keep track of where it left off in previous runs and to navigate through paginated search results effectively. By maintaining the starting page numbers, the scraper can resume from the last visited page in case of interruptions, ensuring that it does not miss any images or re-scrape already processed pages. The total pages provide insight into the scope of the scraping task for each category, allowing the scraper to plan its navigation through the search results effectively and to manage expectations regarding the volume of data that will be collected for each category.
    /// </summary>
    public int StartingPageNumber { get; set; }

    /// <summary>
    /// The total pages for search results, subscriptions, and top wallpapers provide insight into the scope of the scraping task for each category, allowing the scraper to plan its navigation through the search results effectively. Knowing the total pages helps in estimating the time required for scraping and in managing expectations regarding the volume of data that will be collected for each category. It also assists in determining when to stop scraping for a category, ensuring that the scraper does not attempt to access non-existent pages and handles pagination gracefully.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// The starting page numbers for subscriptions and top wallpapers are crucial parameters for managing the pagination of the scraping process specifically for these categories. These properties allow the scraper to keep track of where it left off in previous runs for subscriptions and top wallpapers, enabling it to resume from the last visited page in case of interruptions. This ensures that the scraper does not miss any images or re-scrape already processed pages within these specific categories, allowing for a more efficient and organized scraping process.
    /// </summary>
    public int SubscriptionsStartingPageNumber { get; set; }

    /// <summary>
    /// The total pages for subscriptions and top wallpapers provide insight into the scope of the scraping task for these specific categories, allowing the scraper to plan its navigation through the search results effectively. Knowing the total pages for subscriptions and top wallpapers helps in estimating the time required for scraping and in managing expectations regarding the volume of data that will be collected for each category. It also assists in determining when to stop scraping for these categories, ensuring that the scraper does not attempt to access non-existent pages and handles pagination gracefully.
    /// </summary>
    public int SubscriptionsTotalPages { get; set; }

    /// <summary>
    /// The starting page number and total pages for top wallpapers are crucial parameters for managing the pagination of the scraping process specifically for the top wallpapers category. These properties allow the scraper to keep track of where it left off in previous runs for top wallpapers, enabling it to resume from the last visited page in case of interruptions. This ensures that the scraper does not miss any images or re-scrape already processed pages within the top wallpapers category, allowing for a more efficient and organized scraping process. Additionally, knowing the total pages for top wallpapers helps in estimating the time required for scraping and in managing expectations regarding the volume of data that will be collected for this category, ensuring that the scraper can navigate through the search results effectively and handle pagination gracefully.
    /// </summary>
    public int TopWallpapersTotalPages { get; set; }

    /// <summary>
    /// The starting page number for top wallpapers is a crucial parameter for managing the pagination of the scraping process specifically for the top wallpapers category. This property allows the scraper to keep track of where it left off in previous runs for top wallpapers, enabling it to resume from the last visited page in case of interruptions. This ensures that the scraper does not miss any images or re-scrape already processed pages within the top wallpapers category, allowing for a more efficient and organized scraping process. Additionally, knowing the starting page number for top wallpapers helps in estimating the time required for scraping and in managing expectations regarding the volume of data that will be collected for this category, ensuring that the scraper can navigate through the search results effectively and handle pagination gracefully.
    /// </summary>
    public int TopWallpapersStartingPageNumber { get; set; }

    /// <summary>
    /// The URL of the login page on the target website.
    /// </summary>
    public string LoginUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether the browser runs in headless mode.
    /// </summary>
    public bool UseHeadless { get; set; }

    /// <summary>
    /// Slow motion delay in milliseconds applied to Playwright operations.
    /// </summary>
    public float? SlowMotionDelay { get; set; }
}

