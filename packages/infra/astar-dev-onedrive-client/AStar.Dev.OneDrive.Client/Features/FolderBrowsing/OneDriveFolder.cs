namespace AStar.Dev.OneDrive.Client.Features.FolderBrowsing;

/// <summary>
///     Represents a folder in OneDrive returned from the Graph API (AM-03).
/// </summary>
public sealed record OneDriveFolder(string Id, string Name, string? ParentId, bool HasChildren);

/// <summary>Factory for <see cref="OneDriveFolder"/>.</summary>
public static class OneDriveFolderFactory
{
    /// <summary>Creates a new <see cref="OneDriveFolder"/>.</summary>
    public static OneDriveFolder Create(string id, string name, string? parentId, bool hasChildren)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new OneDriveFolder(id, name, parentId, hasChildren);
    }
}
