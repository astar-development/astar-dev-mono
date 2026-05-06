namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="ItemPath"/>.</summary>
public static class ItemPathFactory
{
    /// <summary>Creates an <see cref="ItemPath"/> from the given name and optional relative path.</summary>
    public static ItemPath Create(string name, string? relativePath = null) => new(name, relativePath);
}
