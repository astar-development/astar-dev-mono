using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// A strongly-typed identifier for a OneDrive item (file or folder) within the sync client. This allows for type safety and clearer code when working with OneDrive items in the synchronization process.
/// </summary>
[StrongId(typeof(string))]
public readonly partial record struct OneDriveItemId;
