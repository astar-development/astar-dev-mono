using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>
/// A strongly-typed identifier for an <see cref="EventEntity"/>.
/// </summary>
[StrongId(typeof(Guid))]
public readonly partial record struct FileEventId;
