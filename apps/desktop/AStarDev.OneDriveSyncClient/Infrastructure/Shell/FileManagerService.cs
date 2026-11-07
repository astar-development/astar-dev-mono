using System.Diagnostics;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <inheritdoc />
public sealed class FileManagerService(IProcessLauncher processLauncher) : IFileManagerService
{
    /// <inheritdoc />
    public void OpenFolder(string path) => processLauncher.Start(CreateStartInfo(path));

    private static ProcessStartInfo CreateStartInfo(string path)
        => new(GetOpener()) { ArgumentList = { path }, UseShellExecute = false };

    private static string GetOpener()
        => OperatingSystem.IsWindows() ? "explorer" : SetAppropriateNonWindowsCommand();

    private static string SetAppropriateNonWindowsCommand() => OperatingSystem.IsMacOS() ? "open" : "xdg-open";
}
