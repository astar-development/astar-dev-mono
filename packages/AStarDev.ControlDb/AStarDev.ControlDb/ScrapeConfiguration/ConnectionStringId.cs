using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>
/// Represents the unique identifier for a connection string entity.
/// </summary>
[StrongId]
public partial record struct ConnectionStringId;
