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

        throw new NotImplementedException($"{processLauncher} {GetOpener()}");
    }

    internal static string GetOpener()
        => OperatingSystem.IsWindows() ? "explorer" : SetAppropriateNonWindowsCommand();

    private static string SetAppropriateNonWindowsCommand() => OperatingSystem.IsMacOS() ? "open" : "xdg-open";
}
