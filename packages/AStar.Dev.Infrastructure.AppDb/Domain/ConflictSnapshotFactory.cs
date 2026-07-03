namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Factory for <see cref="ConflictSnapshot"/>.</summary>
public static class ConflictSnapshotFactory
{
    /// <summary>Creates a <see cref="ConflictSnapshot"/> from the local and remote file state.</summary>
    public static ConflictSnapshot Create(DateTimeOffset localModified, long localSize, DateTimeOffset remoteModified, long remoteSize) => new(localModified, localSize, remoteModified, remoteSize);
}
