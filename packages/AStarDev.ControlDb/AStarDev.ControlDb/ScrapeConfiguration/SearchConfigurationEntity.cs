namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>
/// Represents a search configuration entity in the database.
/// </summary>
public class SearchConfigurationEntity : AuditableEntity
{
    private SearchConfigurationEntity() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchConfigurationEntity"/> class with the specified parameters.
    /// </summary>
    /// <param name="searchConfigurationId">The unique identifier for the search configuration entity.</param>
    /// <param name="scrapeConfigurationId">The unique identifier for the associated scrape configuration entity.</param>
    /// <param name="searchTerm">The search term used for scraping wallpapers.</param>
    /// <param name="maxResults">The maximum number of results to retrieve for the search configuration entity.</param>
    /// <param name="searchCategories">The list of search categories associated with the search configuration entity.</param>
    public SearchConfigurationEntity(SearchConfigurationId searchConfigurationId, ScrapeConfigurationId scrapeConfigurationId, string searchTerm,  int maxResults, ICollection<SearchCategoryEntity> searchCategories)
    {
        Id = searchConfigurationId;
        ScrapeConfigurationId = scrapeConfigurationId;
        SearchTerm = searchTerm;
        MaxResults = maxResults;
        SearchCategories = searchCategories;
    }

    /// <summary>
    /// Gets or sets the unique identifier for the search configuration entity.
    /// </summary>
    public SearchConfigurationId Id { get; init; } = new SearchConfigurationId(Guid.Empty);

    /// <summary>
    /// Gets or sets the unique identifier for the associated scrape configuration entity.
    /// </summary>
    public ScrapeConfigurationId ScrapeConfigurationId { get; init; } = new ScrapeConfigurationId(Guid.Empty);

    /// <summary>
    /// Gets or sets the search term used for scraping wallpapers.
    /// </summary>
    public string SearchTerm { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of results to retrieve for the search configuration entity. This property is optional and can be null.
    /// </summary>
    public int? MaxResults { get; init; }

    /// <summary>The base URL of the target website.</summary>
    public Uri BaseUrl { get; set; } = new("https://example.com");

    /// <summary>The API key used to authenticate requests to the target website.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>The categories the scraper targets.</summary>
    public ICollection<SearchCategoryEntity> SearchCategories { get; } = [];

    /// <summary>The search query used to find content.</summary>
    public string SearchString { get; set; } = string.Empty;

    /// <summary>The category or section featuring the most popular content.</summary>
    public string TopWallpapers { get; set; } = string.Empty;

    /// <summary>A prefix applied before the search string.</summary>
    public string SearchStringPrefix { get; set; } = string.Empty;

    /// <summary>A suffix applied after the search string.</summary>
    public string SearchStringSuffix { get; set; } = string.Empty;

    /// <summary>The subscriptions section on the target website.</summary>
    public string Subscriptions { get; set; } = string.Empty;

    /// <summary>The pause, in seconds, between processing individual images.</summary>
    public int ImagePauseInSeconds { get; set; }

    /// <summary>The page number to resume search results from.</summary>
    public int StartingPageNumber { get; set; }

    /// <summary>The total number of pages available for search results.</summary>
    public int TotalPages { get; set; }

    /// <summary>The page number to resume subscriptions from.</summary>
    public int SubscriptionsStartingPageNumber { get; set; }

    /// <summary>The total number of pages available for subscriptions.</summary>
    public int SubscriptionsTotalPages { get; set; }

    /// <summary>The page number to resume top wallpapers from.</summary>
    public int TopWallpapersStartingPageNumber { get; set; }

    /// <summary>The total number of pages available for top wallpapers.</summary>
    public int TopWallpapersTotalPages { get; set; }

    /// <summary>The URL of the login page on the target website.</summary>
    public Uri LoginUrl { get; set; } = new("https://example.com/login");

    /// <summary>Whether the browser runs in headless mode.</summary>
    public bool UseHeadless { get; set; }

    /// <summary>Slow motion delay in milliseconds applied to browser automation.</summary>
    public float? SlowMotionDelay { get; set; }
}
