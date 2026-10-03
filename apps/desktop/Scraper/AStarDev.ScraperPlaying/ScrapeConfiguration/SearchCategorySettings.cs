namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>The validated editable values of one search category.</summary>
/// <param name="Id">The category identifier, used to build the category URL. Matched case-insensitively.</param>
/// <param name="Name">The human-readable name of the category.</param>
/// <param name="IncludeInSearch">Whether the category is included in the scraping process.</param>
/// <param name="IsFamous">Whether the category defines a famous person.</param>
/// <param name="IsInternet">Whether the category defines the internet classification.</param>
public sealed record SearchCategorySettings(string Id, string Name, bool IncludeInSearch, bool IsFamous, bool IsInternet);
