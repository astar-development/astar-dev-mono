using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// A strongly-typed identifier for a <see cref="ModelToIgnoreEntity"/>.
/// </summary>
[StrongId(typeof(Guid))]
public readonly partial record struct ModelId;
