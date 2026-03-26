namespace AStar.Dev.Sync.Engine;

/// <summary>
/// Indicates the direction of a file transfer during sync.
/// </summary>
public enum SyncDirection
{
    /// <summary>Upload a local file to the remote provider.</summary>
    LocalToRemote,

    /// <summary>Download a remote file to the local file system.</summary>
    RemoteToLocal
}
