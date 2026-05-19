using System.Diagnostics;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <inheritdoc />
public sealed class FileManagerService : IFileManagerService
{
    /// <inheritdoc />
    public void OpenFolder(string path)
    {
        string opener = OperatingSystem.IsWindows() ? "explorer"
                      : OperatingSystem.IsMacOS()   ? "open"
                      : "xdg-open";

        _ = Process.Start(opener, path);
    }
}
