using AStarDev.SourceGeneratorAttributes;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// A strongly-typed identifier for a <see cref="TagToIgnoreEntity"/>.
/// </summary>
[StrongId(typeof(Guid))]
public readonly partial record struct TagId;
