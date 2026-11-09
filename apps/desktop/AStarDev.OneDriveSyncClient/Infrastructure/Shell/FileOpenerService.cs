using System.Diagnostics;
using System.IO.Abstractions;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <inheritdoc />
public sealed class FileOpenerService(IProcessLauncher processLauncher, IFileSystem fileSystem) : IFileOpenerService
{
    /// <inheritdoc />
    public void OpenFile(string localPath)
    {
        if (!fileSystem.File.Exists(localPath))
            return;

        processLauncher.Start(new ProcessStartInfo(GetOpener()) { ArgumentList = { localPath }, UseShellExecute = false });
    }

    private static string GetOpener()
        => OperatingSystem.IsWindows() ? "explorer" : SetAppropriateNonWindowsCommand();

    private static string SetAppropriateNonWindowsCommand() => OperatingSystem.IsMacOS() ? "open" : "xdg-open";
}
