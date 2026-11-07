using System.Diagnostics;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <inheritdoc />
public sealed class FileManagerService(IProcessLauncher processLauncher) : IFileManagerService
{
    /// <inheritdoc />
    public void OpenFolder(string path) => throw new NotImplementedException($"{processLauncher} {CreateStartInfo(path).FileName}");

    internal static ProcessStartInfo CreateStartInfo(string path)
        => new(GetOpener()) { ArgumentList = { path }, UseShellExecute = false };

    internal static string GetOpener()
        => OperatingSystem.IsWindows() ? "explorer" : SetAppropriateNonWindowsCommand();

    private static string SetAppropriateNonWindowsCommand() => OperatingSystem.IsMacOS() ? "open" : "xdg-open";
}
