namespace AStar.Dev.OneDrive.Sync.Client.Models;

/// <summary>
/// Determines how bidirectional sync conflicts are resolved.
/// A conflict occurs when both the local and remote versions of a file
/// have been modified since the last successful sync.
/// </summary>
public enum ConflictPolicy
{
    /// <summary>Skip the file — leave both versions unchanged. Default.</summary>
    Ignore = 0,

    /// <summary>Keep both versions — rename the local copy with a suffix.</summary>
    KeepBoth = 1,

    /// <summary>The most recently modified version wins.</summary>
    LastWriteWins = 2,

    /// <summary>The local version always wins — remote is overwritten.</summary>
    LocalWins = 3,

    /// <summary>The remote version always wins — local is overwritten.</summary>
    RemoteWins = 4
}
