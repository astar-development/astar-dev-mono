using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>
/// Represents the unique identifier for a search configuration entity.
/// </summary>
[StrongId]
public partial record struct SearchConfigurationId;
