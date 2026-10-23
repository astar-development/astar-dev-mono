using System.Diagnostics;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <inheritdoc />
public sealed class FileManagerService : IFileManagerService
{
    /// <inheritdoc />
    public void OpenFolder(string path) => _ = Process.Start(CreateStartInfo(path));

    internal static ProcessStartInfo CreateStartInfo(string path)
        => new(GetOpener()) { ArgumentList = { path }, UseShellExecute = false };

    internal static string GetOpener()
        => OperatingSystem.IsWindows() ? "explorer" : SetAppropriateNonWindowsCommand();

    private static string SetAppropriateNonWindowsCommand() => OperatingSystem.IsMacOS() ? "open" : "xdg-open";
}
