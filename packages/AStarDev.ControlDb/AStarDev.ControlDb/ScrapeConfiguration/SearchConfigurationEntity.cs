namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>Represents a search configuration entity in the database.</summary>
public class SearchConfigurationEntity : AuditableEntity
{
    private SearchConfigurationEntity() { }

    /// <summary>Initializes a new instance of the <see cref="SearchConfigurationEntity"/> class with the specified parameters.</summary>
    /// <param name="searchConfigurationId">The unique identifier for the search configuration entity.</param>
    /// <param name="scrapeConfigurationId">The unique identifier for the associated scrape configuration entity.</param>
    /// <param name="searchTerm">The search term used for scraping wallpapers.</param>
    /// <param name="maxResults">The maximum number of results to retrieve, or null for no limit.</param>
    /// <param name="searchCategories">The list of search categories associated with the search configuration entity.</param>
    public SearchConfigurationEntity(SearchConfigurationId searchConfigurationId, ScrapeConfigurationId scrapeConfigurationId, string searchTerm, int? maxResults, ICollection<SearchCategoryEntity> searchCategories)
    {
        Id = searchConfigurationId;
        ScrapeConfigurationId = scrapeConfigurationId;
        SearchTerm = searchTerm;
        MaxResults = maxResults;
        SearchCategories = searchCategories;
    }

    /// <summary>Gets or sets the unique identifier for the search configuration entity.</summary>
    public SearchConfigurationId Id { get; init; } = new SearchConfigurationId(Guid.Empty);

    /// <summary>Gets or sets the unique identifier for the associated scrape configuration entity.</summary>
    public ScrapeConfigurationId ScrapeConfigurationId { get; init; } = new ScrapeConfigurationId(Guid.Empty);

    /// <summary>Gets or sets the search term used for scraping wallpapers.</summary>
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>Gets or sets the maximum number of results to retrieve for the search configuration entity. This property is optional and can be null.</summary>
    public int? MaxResults { get; set; }

    /// <summary>The categories the scraper targets.</summary>
    public ICollection<SearchCategoryEntity> SearchCategories { get; } = [];

    /// <summary>The Wallhaven tag categories whose tags are treated as person names when naming wallpaper files.</summary>
    public ICollection<PersonCategoryEntity> PersonCategories { get; } = [];
}
