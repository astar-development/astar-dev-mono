namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Defines the policies for resolving file conflicts during synchronization between local and remote OneDrive storage.
/// </summary>
public enum ConflictPolicy
{
    /// <summary>
    /// Ignore the conflict and do not synchronize the file. The file will remain unchanged in both local and remote storage.
    /// </summary>
    Ignore = 0,

    /// <summary>
    /// Keep both versions of the file by renaming one of them. This allows both the local and remote versions to coexist without overwriting each other.
    /// </summary>
    KeepBoth = 1,

    /// <summary>
    /// The version of the file with the most recent modification timestamp wins. The older version will be overwritten.
    /// </summary>
    LastWriteWins = 2,

    /// <summary>
    /// The local version of the file always wins — remote is overwritten. This means that any changes made to the file in remote storage will be discarded in favor of the local version during synchronization.
    /// </summary>
    LocalWins = 3,

    /// <summary>
    /// The remote version of the file always wins — local is overwritten. This means that any changes made to the file in local storage will be discarded in favor of the remote version during synchronization.
    /// </summary>
    RemoteWins = 4
}
