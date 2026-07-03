using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// A strongly-typed identifier for a OneDrive account within the sync client.
/// </summary>
[StrongId(typeof(string))]
public readonly partial record struct AccountId;
