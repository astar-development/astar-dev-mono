using AStarDev.SourceGenerators.Attributes;

namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>
/// Represents the unique identifier for a connection strings entity.
/// </summary>
[StrongId]
public partial record struct ConnectionStringsId;
