using System.Diagnostics;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <inheritdoc />
public sealed class FileOpenerService : IFileOpenerService
{
    /// <inheritdoc />
    public void OpenFile(string localPath)
    {
        if (!File.Exists(localPath))
            return;

        _ = Process.Start(new ProcessStartInfo(GetOpener()) { ArgumentList = { localPath }, UseShellExecute = false });
    }

    internal static string GetOpener()
        => OperatingSystem.IsWindows() ? "explorer"
         : OperatingSystem.IsMacOS()   ? "open"
         : "xdg-open";
}
