using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Persistence;

/// <summary>
///     Returns the platform-appropriate local application data directory as the
///     application data root for OneDrive Sync.
///
///     <list type="bullet">
///         <item><description>Linux: <c>~/.local/share/astar-dev/</c></description></item>
///         <item><description>Windows: <c>%LOCALAPPDATA%\astar-dev\</c></description></item>
///         <item><description>macOS: <c>~/Library/Application Support/astar-dev/</c></description></item>
///     </list>
///
///     All three resolve via <see cref="Environment.SpecialFolder.LocalApplicationData" />,
///     so this implementation is cross-platform by construction even though it is tested
///     only on Linux in the MVP.
/// </summary>
public sealed class LocalApplicationPathsProvider : IApplicationPathsProvider
{
    /// <inheritdoc />
    public string ApplicationDirectory => GetPlatformDataDirectory();

    /// <inheritdoc />
    public string LogsDirectory => ResolveLogDirectory();

    /// <inheritdoc />
    public string UserDataDirectory => ResolveUsersDirectory();

    private static string ResolveLogDirectory()
    {
        string logDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).CombinePath(ApplicationMetadata.ApplicationName, "logs");

        _ = Directory.CreateDirectory(logDirectory);

        return logDirectory;
    }

    private static string ResolveUsersDirectory()
    {
        string logDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).CombinePath(ApplicationMetadata.ApplicationName, "logs");

        _ = Directory.CreateDirectory(logDirectory);

        return logDirectory;
    }

    private static string GetPlatformDataDirectory()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string directory= OperatingSystem.IsWindows()
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ApplicationMetadata.ApplicationName)
            : OperatingSystem.IsMacOS()
                ? Path.Combine(home, "Library", "Application Support", ApplicationMetadata.ApplicationName)
                : Path.Combine(home, ".config", ApplicationMetadata.ApplicationName);
        _ = Directory.CreateDirectory(directory);

        return directory;
    }
}

