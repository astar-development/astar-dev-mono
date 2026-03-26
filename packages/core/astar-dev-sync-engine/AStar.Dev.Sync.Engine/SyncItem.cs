namespace AStar.Dev.Sync.Engine;

/// <summary>
/// Represents a single file that needs to be synchronised.
/// </summary>
public sealed class SyncItem
{
    /// <summary>Relative path of the file within the sync root (forward-slash separated).</summary>
    public required string RelativePath { get; init; }

    /// <summary>The direction this item should be synced.</summary>
    public required SyncDirection Direction { get; init; }

    /// <summary>Last-modified timestamp of the local copy, if it exists.</summary>
    public DateTimeOffset? LocalModifiedUtc { get; init; }

    /// <summary>Last-modified timestamp of the remote copy, if it exists.</summary>
    public DateTimeOffset? RemoteModifiedUtc { get; init; }

    /// <summary>Size of the local file in bytes, if it exists.</summary>
    public long? LocalSizeBytes { get; init; }

    /// <summary>Size of the remote file in bytes, if it exists.</summary>
    public long? RemoteSizeBytes { get; init; }
}
