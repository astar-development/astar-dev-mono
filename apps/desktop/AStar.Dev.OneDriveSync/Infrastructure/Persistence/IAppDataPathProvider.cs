namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Abstracts the platform-specific application data directory so that
///     <see cref="DbBackupService" /> and startup migration code remain testable
///     without touching the real file system.
/// </summary>
public interface IAppDataPathProvider
{
    /// <summary>
    ///     Absolute path to the directory that holds the application's persistent data files.
    ///     Example (Linux): <c>~/.local/share/AStar.Dev.OneDriveSync/</c>
    /// </summary>
    string AppDataDirectory { get; }
}
