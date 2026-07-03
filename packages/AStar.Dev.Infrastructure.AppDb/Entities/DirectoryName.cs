namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>The directory path containing a file, excluding the file name.</summary>
/// <param name="Value">The directory path.</param>
public readonly record struct DirectoryName(string Value);
