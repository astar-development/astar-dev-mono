using AStarDev.SourceGenerators.Attributes;

namespace AStarDev.ControlDb.Files;

/// <summary>
/// Represents the unique identifier for a file entity.
/// </summary>
[StrongId]
public readonly partial record struct FileId;
