using System.Diagnostics;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <inheritdoc />
public sealed class FileManagerService : IFileManagerService
{
    /// <inheritdoc />
    public void OpenFolder(string path)
    {
        string opener = OperatingSystem.IsWindows() ? "explorer" : SetAppropriateNonWindowsCommand();

        _ = Process.Start(opener, path);
    }

    private static string SetAppropriateNonWindowsCommand() => OperatingSystem.IsMacOS() ? "open" : "xdg-open";
}
