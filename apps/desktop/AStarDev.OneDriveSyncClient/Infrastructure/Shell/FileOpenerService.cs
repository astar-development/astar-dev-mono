using System.Diagnostics;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <inheritdoc />
public sealed class FileOpenerService(IProcessLauncher processLauncher) : IFileOpenerService
{
    /// <inheritdoc />
    public void OpenFile(string localPath)
    {
        if (!File.Exists(localPath))
            return;

        processLauncher.Start(new ProcessStartInfo(GetOpener()) { ArgumentList = { localPath }, UseShellExecute = false });
    }

    private static string GetOpener()
        => OperatingSystem.IsWindows() ? "explorer" : SetAppropriateNonWindowsCommand();

    private static string SetAppropriateNonWindowsCommand() => OperatingSystem.IsMacOS() ? "open" : "xdg-open";
}
