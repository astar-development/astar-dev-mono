using AStarDev.SourceGeneratorAttributes;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Strongly-typed identifier for a Microsoft Graph drive-item folder.</summary>
[StrongType(typeof(string))]
public readonly partial record struct OneDriveFolderId;
