using System.Diagnostics;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

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
        => OperatingSystem.IsWindows() ? "explorer" : SetAppropriateNonWindowsCommand();

    private static string SetAppropriateNonWindowsCommand() => OperatingSystem.IsMacOS() ? "open" : "xdg-open";
}
