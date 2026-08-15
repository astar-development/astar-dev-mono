namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>
/// Represents a connection strings entity in the database.
/// </summary>
/// <param name="Id">The unique identifier for the connection strings entity.</param>
/// <param name="ScrapeConfigurationEntityId">The unique identifier for the associated scrape configuration entity.</param>
/// <param name="Sqlite">The SQLite connection string.</param>
public record ConnectionStringsEntity(ConnectionStringId Id, ScrapeConfigurationId ScrapeConfigurationEntityId, string Sqlite);
