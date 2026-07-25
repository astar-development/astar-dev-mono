using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Factory for <see cref="SyncFileMetadata"/>.</summary>
public static class SyncFileMetadataFactory
{
    /// <summary>Creates a <see cref="SyncFileMetadata"/> with no version information.</summary>
    public static SyncFileMetadata Create(long fileSize, DateTimeOffset remoteModified) => new(fileSize, remoteModified, Option.None<VersionInfo>());

    /// <summary>Creates a <see cref="SyncFileMetadata"/> with the given file attributes and version information.</summary>
    public static SyncFileMetadata Create(long fileSize, DateTimeOffset remoteModified, Option<VersionInfo> versionInfo) => new(fileSize, remoteModified, versionInfo);
}
