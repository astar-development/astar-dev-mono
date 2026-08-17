namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Factory for <see cref="SyncFileTarget"/>.</summary>
public static class SyncFileTargetFactory
{
    /// <summary>Creates a <see cref="SyncFileTarget"/> for the given local paths.</summary>
    public static SyncFileTarget Create(string localPath, string relativePath) => new(localPath, relativePath);
}
