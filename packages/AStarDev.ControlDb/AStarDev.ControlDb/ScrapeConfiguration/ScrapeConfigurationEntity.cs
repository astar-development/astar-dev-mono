namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>
/// Represents a scrape configuration entity in the database.
/// </summary>
/// <param name="Id">The unique identifier for the scrape configuration entity.</param>
public record ScrapeConfigurationEntity(ScrapeConfigurationId Id)
{
    /// <summary>The connection strings associated with the scrape configuration.</summary>
    public ConnectionStringsEntity ConnectionStrings { get; init; } = null!;

    /// <summary>The user configuration associated with the scrape configuration.</summary>
    public UserConfigurationEntity UserConfiguration { get; init; } = null!;

    /// <summary>The search configuration associated with the scrape configuration.</summary>
    public SearchConfigurationEntity SearchConfiguration { get; init; } = null!;

    /// <summary>The scrape directories associated with the scrape configuration.</summary>
    public ScrapeDirectoriesEntity ScrapeDirectories { get; init; } = null!;
}
