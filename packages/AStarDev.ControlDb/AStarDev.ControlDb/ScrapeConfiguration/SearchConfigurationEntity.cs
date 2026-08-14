namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>
/// Represents a search configuration entity in the database.
/// </summary>
/// <param name="Id">The unique identifier for the search configuration entity.</param>
/// <param name="ScrapeConfigurationEntityId">The unique identifier for the associated scrape configuration entity.</param>
/// <param name="SearchTerm">The search term for the configuration.</param>
/// <param name="Category">The category for the search.</param>
/// <param name="MaxResults">The maximum number of results to return.</param>
public record SearchConfigurationEntity(SearchConfigurationId Id, ScrapeConfigurationId ScrapeConfigurationEntityId, string SearchTerm, string? Category, int? MaxResults);
