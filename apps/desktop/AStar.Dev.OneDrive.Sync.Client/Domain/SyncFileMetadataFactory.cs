using AStar.Dev.OneDrive.Sync.Client.Data.Entities;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="SyncFileMetadata"/>.</summary>
public static class SyncFileMetadataFactory
{
    /// <summary>Creates a <see cref="SyncFileMetadata"/> from the given file attributes.</summary>
    public static SyncFileMetadata Create(long fileSize, DateTimeOffset remoteModified, VersionInfo? versionInfo = null) => new(fileSize, remoteModified, versionInfo);
}
