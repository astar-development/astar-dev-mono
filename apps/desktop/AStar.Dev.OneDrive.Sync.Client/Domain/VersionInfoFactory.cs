namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="VersionInfo"/>.</summary>
public static class VersionInfoFactory
{
    /// <summary>Creates a <see cref="VersionInfo"/> from the given ETag and CTag values.</summary>
    public static VersionInfo Create(string? eTag, string? cTag) => new(eTag, cTag);
}
