using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Strongly-typed identifier for a Microsoft Graph drive-item folder.</summary>
[StrongId(typeof(string))]
public readonly partial record struct OneDriveFolderId;
