namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>
/// Factory for creating instances of <see cref="VersionInfo"/> with default or specified values.
/// </summary>
public static class VersionInfoFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="VersionInfo"/> with the provided ETag and CTag values. This method allows for the creation of version information based on specific versioning tags, enabling the sync client to track changes to OneDrive items effectively. Both parameters are nullable to accommodate cases where version information may not be available or applicable, such as for new items that have not yet been synchronized or for items that do not support versioning.
    /// </summary>
    /// <param name="eTag">The ETag for the item.</param>
    /// <param name="cTag">The CTag for the item.</param>
    /// <returns>A new instance of <see cref="VersionInfo"/>.</returns>
    public static VersionInfo Create(string? eTag, string? cTag) => new(eTag, cTag);
}
