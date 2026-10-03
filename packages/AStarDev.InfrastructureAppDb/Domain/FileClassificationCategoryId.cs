using AStarDev.SourceGeneratorAttributes;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Strongly-typed identifier for a file classification category.</summary>
[StrongType(typeof(int))]
public readonly partial record struct FileClassificationCategoryId;
