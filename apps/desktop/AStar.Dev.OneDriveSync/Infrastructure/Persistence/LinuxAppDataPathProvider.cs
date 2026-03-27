namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Returns <c>~/.local/share/AStar.Dev.OneDriveSync/</c> as the application data
///     directory on Linux (<see cref="Environment.SpecialFolder.LocalApplicationData" />).
///
///     On Windows and macOS the same special folder resolves to the platform-conventional
///     location, so this implementation is cross-platform by construction even though it
///     is tested only on Linux in the MVP.
/// </summary>
public sealed class LinuxAppDataPathProvider : IAppDataPathProvider
{
    private static readonly string ResolvedPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AStar.Dev.OneDriveSync");

    public string AppDataDirectory => ResolvedPath;
}
