namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>The search parameters guiding the scraper's navigation of the target website.</summary>
public sealed class SearchConfigurationEntity : AuditableEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the parent scrape configuration.</summary>
    public int ScrapeConfigurationEntityId { get; set; }

    /// <summary>Navigation property to the parent scrape configuration.</summary>
    public ScrapeConfigurationEntity? ScrapeConfigurationEntity { get; set; }

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
