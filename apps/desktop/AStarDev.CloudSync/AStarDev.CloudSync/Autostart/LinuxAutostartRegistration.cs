using System.IO.Abstractions;
using AStarDev.CloudSync.Sync;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Autostart;

/// <summary>Registers the app to start at login with an XDG autostart entry in <c>~/.config/autostart</c> (#1410). The entry is the setting: it exists or it does not.</summary>
/// <param name="fileSystem">Reads and writes the entry.</param>
/// <param name="home">Supplies the user's home folder.</param>
/// <param name="launcher">Supplies the program to start, or nothing for a build that is not installed.</param>
/// <param name="logger">Reports a failure by exception type only.</param>
public sealed class LinuxAutostartRegistration(IFileSystem fileSystem, IHomeFolder home, IAutostartLauncher launcher, ILogger<LinuxAutostartRegistration> logger) : FileAutostartRegistration(fileSystem, home, launcher, logger)
{
    /// <inheritdoc />
    protected override string[] EntryPath => [".config", "autostart", "astardev-cloudsync.desktop"];

    /// <inheritdoc />
    protected override string BuildEntry(string launcherPath) => DesktopEntryBuilder.Build(launcherPath);
}
