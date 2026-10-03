using AStarDev.SourceGeneratorAttributes;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// A strongly-typed identifier for an <see cref="EventEntity"/>.
/// </summary>
[StrongType(typeof(Guid))]
public readonly partial record struct FileEventId;
