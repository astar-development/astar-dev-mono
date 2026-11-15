namespace AStarDev.CloudSync.Logging;

/// <summary>Works out the log folder for a platform (#1411).</summary>
public static class LogFolderResolver
{
    /// <summary>The log folder: <c>~/.local/share/AStarDev.CloudSync/logs</c> on Linux, <c>%LOCALAPPDATA%\AStarDev.CloudSync\logs</c> on Windows, <c>~/Library/Logs/AStarDev.CloudSync</c> on macOS.</summary>
    /// <param name="environment">The platform and the user's folders.</param>
    public static string Resolve(LogEnvironment environment) => throw new NotImplementedException($"{environment}");
}
