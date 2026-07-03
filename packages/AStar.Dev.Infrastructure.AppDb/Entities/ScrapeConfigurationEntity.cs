namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>The root configuration entity for the scraper, aggregating connection, user, search, and directory settings.</summary>
public sealed class ScrapeConfigurationEntity : AuditableEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>The connection strings required by the scraper.</summary>
    public ConnectionStringsEntity ConnectionStrings { get; set; } = new();

    /// <summary>The user authentication details for the target website.</summary>
    public UserConfigurationEntity UserConfiguration { get; set; } = new();

    /// <summary>The search parameters for the scraper.</summary>
    public SearchConfigurationEntity SearchConfiguration { get; set; } = new();

    /// <summary>The directories the scraper reads from and writes to.</summary>
    public ScrapeDirectoriesEntity ScrapeDirectories { get; set; } = new();
}
