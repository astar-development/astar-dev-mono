using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>
/// Factory for creating instances of <see cref="VersionInfo"/>.
/// </summary>
public static class VersionInfoFactory
{
    /// <summary>Creates a new instance of <see cref="VersionInfo"/> with the provided ETag and CTag values.</summary>
    /// <param name="eTag">The ETag for the item, or None if not available.</param>
    /// <param name="cTag">The CTag for the item, or None if not available.</param>
    public static VersionInfo Create(Option<string> eTag, Option<string> cTag) => new(eTag, cTag);
}
