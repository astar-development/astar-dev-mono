using System.Diagnostics;
using System.IO.Abstractions;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <inheritdoc />
public sealed class FileOpenerService(IProcessLauncher processLauncher, IFileSystem fileSystem) : IFileOpenerService
{
    /// <inheritdoc />
    public void OpenFile(string localPath) => throw new NotImplementedException($"{localPath}{fileSystem}{processLauncher}{GetOpener()}");

    private static string GetOpener()
        => OperatingSystem.IsWindows() ? "explorer" : SetAppropriateNonWindowsCommand();

    private static string SetAppropriateNonWindowsCommand() => OperatingSystem.IsMacOS() ? "open" : "xdg-open";
}
