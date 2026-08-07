using AStarDev.SourceGenerators.Attributes;

namespace AStarDev.FilesDb;

/// <summary>
/// Represents the unique identifier for a file entity.
/// </summary>
[StrongId]
public readonly partial record struct FileId;
