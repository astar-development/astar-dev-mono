using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>
/// A strongly-typed identifier for a <see cref="ScrapedTagEntity"/>.
/// </summary>
[StrongId(typeof(Guid))]
public readonly partial record struct ScrapedTagId;
