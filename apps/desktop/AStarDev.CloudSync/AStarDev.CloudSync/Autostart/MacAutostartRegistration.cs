using System.IO.Abstractions;
using AStarDev.CloudSync.Sync;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Autostart;

/// <summary>Registers the app to start at login with a LaunchAgent plist in <c>~/Library/LaunchAgents</c> (#1410). The file is the setting: it exists or it does not.</summary>
/// <param name="fileSystem">Reads and writes the plist.</param>
/// <param name="home">Supplies the user's home folder.</param>
/// <param name="launcher">Supplies the program to start, or nothing for a build that is not installed.</param>
/// <param name="logger">Reports a failure by exception type only.</param>
public sealed class MacAutostartRegistration(IFileSystem fileSystem, IHomeFolder home, IAutostartLauncher launcher, ILogger<MacAutostartRegistration> logger) : FileAutostartRegistration(fileSystem, home, launcher, logger)
{
    /// <inheritdoc />
    protected override string[] EntryPath => ["Library", "LaunchAgents", LaunchAgentPlistBuilder.Label + ".plist"];

    /// <inheritdoc />
    protected override string BuildEntry(string launcherPath) => LaunchAgentPlistBuilder.Build(launcherPath);
}
