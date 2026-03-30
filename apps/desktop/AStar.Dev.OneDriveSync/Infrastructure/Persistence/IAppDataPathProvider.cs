namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Abstracts the platform-specific application data directory so that
///     <see cref="DbBackupService" /> and startup migration code remain testable
///     without touching the real file system.
/// </summary>
public interface IAppDataPathProvider
{
    /// <summary>
    ///     Absolute path to the shared AStar.Dev application data directory.
    ///     Example (Linux): <c>~/.local/share/astar-dev/</c>
    /// </summary>
    string AppDataDirectory { get; }
}
