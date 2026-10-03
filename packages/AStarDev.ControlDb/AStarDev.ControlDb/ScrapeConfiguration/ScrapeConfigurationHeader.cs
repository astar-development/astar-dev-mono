namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>The few fields of a scrape configuration needed to list and label it, without loading the rest of it.</summary>
/// <param name="Id">The configuration's identifier.</param>
/// <param name="BaseUrl">The site the configuration scrapes.</param>
/// <param name="SearchTerm">The configuration's search term, empty when it has none.</param>
public sealed record ScrapeConfigurationHeader(ScrapeConfigurationId Id, Uri BaseUrl, string SearchTerm);
