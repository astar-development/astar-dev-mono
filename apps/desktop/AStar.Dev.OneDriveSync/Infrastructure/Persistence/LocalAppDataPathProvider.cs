using AStar.Dev.Utilities;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

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
public sealed class LocalAppDataPathProvider : IAppDataPathProvider
{
    private static readonly string _resolvedPath = Environment
        .GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        .CombinePath("astar-dev");

    public string AppDataDirectory => _resolvedPath;
}
