using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.ControlDb.Files;

/// <summary>
/// Represents the unique identifier for a file access detail entity.
/// </summary>
[StrongId]
public readonly partial record struct FileAccessDetailId;
