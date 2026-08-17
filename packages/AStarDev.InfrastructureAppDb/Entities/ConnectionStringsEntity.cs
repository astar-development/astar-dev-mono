namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>The connection strings required by the scraper to reach its databases and services.</summary>
public sealed class ConnectionStringsEntity : AuditableEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the parent scrape configuration.</summary>
    public int ScrapeConfigurationEntityId { get; set; }

    /// <summary>Navigation property to the parent scrape configuration.</summary>
    public ScrapeConfigurationEntity? ScrapeConfigurationEntity { get; set; }

    /// <summary>The SQLite connection string used to store scraped data.</summary>
    public string Sqlite { get; set; } = string.Empty;
}
