namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Represents a OneDrive drive ID, providing type safety over raw strings.</summary>
/// <param name="Value">The string value of the drive ID.</param>
public readonly record struct DriveId(string Value);
