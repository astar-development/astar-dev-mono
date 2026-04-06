namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Persistence;

/// <summary>
///     Abstracts the platform-specific application data directory so that
///     <see cref="DbBackupService" /> and startup migration code remain testable
///     without touching the real file system.
/// </summary>
public interface IApplicationPathsProvider
{
    /// <summary>
    ///     Absolute path to the shared AStar.Dev application data directory.
    ///     Example (Linux): <c>~/.local/share/astar-dev/</c>
    ///     The directory will be created if it does not exist.
    /// </summary>
    string ApplicationDirectory { get; }

    /// <summary>
    ///     Absolute path to the log directory.
    ///     The directory will be created if it does not exist.
    /// </summary>
    string LogsDirectory { get; }

    /// <summary>
    ///     Absolute path to the default Users data directory.
    ///     The directory will be created if it does not exist.
    /// </summary>
    string UserDataDirectory { get; }
}
