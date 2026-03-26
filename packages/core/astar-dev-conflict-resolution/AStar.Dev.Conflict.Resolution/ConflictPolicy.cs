namespace AStar.Dev.Conflict.Resolution;

/// <summary>
/// Resolution strategies available to the user when a conflict is detected.
/// </summary>
public enum ConflictPolicy
{
    /// <summary>Keep the local file, discard the remote version.</summary>
    LocalWins,

    /// <summary>Keep the remote file, discard the local version.</summary>
    RemoteWins,

    /// <summary>Rename the conflicting copy and keep both files.</summary>
    KeepBoth,

    /// <summary>Defer this conflict for later resolution.</summary>
    Skip
}
