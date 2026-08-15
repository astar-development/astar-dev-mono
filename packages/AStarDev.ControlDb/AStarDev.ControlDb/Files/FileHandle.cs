namespace AStarDev.ControlDb.Files;

/// <summary>An opaque, stable handle identifying a file across renames and moves.</summary>
/// <param name="Value">The handle value.</param>
public readonly record struct FileHandle(string Value);
