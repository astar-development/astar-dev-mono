using AStarDev.SourceGenerators.Attributes;

namespace AStarDev.ControlDb.Files;

/// <summary>
/// Represents the unique identifier for a file deletion status entity.
/// </summary>
[StrongId]
public readonly partial record struct DeletionStatusId;
