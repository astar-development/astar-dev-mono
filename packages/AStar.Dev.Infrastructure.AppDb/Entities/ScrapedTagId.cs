using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// A strongly-typed identifier for a <see cref="ScrapedTagEntity"/>.
/// </summary>
[StrongId(typeof(Guid))]
public readonly partial record struct ScrapedTagId;
