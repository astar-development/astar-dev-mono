using AStarDev.SourceGeneratorAttributes;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// A strongly-typed identifier for a OneDrive account within the sync client.
/// </summary>
[StrongType(typeof(string))]
public readonly partial record struct AccountId;
