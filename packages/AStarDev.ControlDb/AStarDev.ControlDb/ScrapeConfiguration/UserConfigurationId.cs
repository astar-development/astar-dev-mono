using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>
/// Represents the unique identifier for a user configuration entity.
/// </summary>
[StrongId]
public partial record struct UserConfigurationId;
