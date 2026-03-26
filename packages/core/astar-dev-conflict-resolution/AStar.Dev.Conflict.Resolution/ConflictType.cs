namespace AStar.Dev.Conflict.Resolution;

/// <summary>
/// Identifies the type of conflict detected during sync.
/// </summary>
public enum ConflictType
{
    /// <summary>The same file was modified on both the local and remote sides.</summary>
    ModifiedBothSides,

    /// <summary>The file was deleted locally but still exists on the remote side.</summary>
    DeletedLocalPresentRemote,

    /// <summary>The file was deleted on the remote side but still exists locally.</summary>
    DeletedRemotePresentLocal
}
