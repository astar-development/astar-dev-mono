namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>Tracks scraping progress for a single search category.</summary>
public sealed class SearchCategoryEntity : AuditableEntity
{
    /// <summary>Foreign key to the parent search configuration.</summary>
    public int SearchConfigurationId { get; set; }

    /// <summary>Navigation property to the parent search configuration.</summary>
    public SearchConfigurationEntity? SearchConfiguration { get; set; }

    /// <summary>The unique identifier for the search category.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The human-readable name of the category.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The number of images observed for this category as of the last scrape.</summary>
    public int LastKnownImageCount { get; set; }

    /// <summary>The last page visited for this category.</summary>
    public int LastPageVisited { get; set; }

    /// <summary>The total number of pages available for this category.</summary>
    public int TotalPages { get; set; }

    /// <summary>Whether this category should be included in the scraping process.</summary>
    public bool IncludeInSearch { get; set; } = true;

    /// <summary>
    ///   Whether this category defines a famous person. This flag can be used to prioritize or filter categories based on their significance or popularity.
    /// </summary>
    public bool IsFamous { get; set; }

    /// <summary>
    ///  Whether this category defines the internet classification. This flag can be used to determine if the category is relevant for internet-based searches or operations.
    /// </summary>
    public bool IsInternet { get; set; }
}
