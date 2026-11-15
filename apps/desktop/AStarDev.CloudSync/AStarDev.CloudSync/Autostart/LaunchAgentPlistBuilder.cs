using System.Security;

namespace AStarDev.CloudSync.Autostart;

/// <summary>Builds the text of the macOS LaunchAgent property list that starts the app at login (#1410).</summary>
public static class LaunchAgentPlistBuilder
{
    /// <summary>The label that names the agent.</summary>
    public const string Label = "dev.astar.cloudsync";

    /// <summary>Builds the plist that starts <paramref name="launcherPath"/> hidden in the tray when the user logs in. The path is XML-escaped and control characters in it are dropped, so a path can never add a key or an argument of its own.</summary>
    /// <param name="launcherPath">The full path of the program.</param>
    public static string Build(string launcherPath) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
        + "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n"
        + "<plist version=\"1.0\">\n<dict>\n"
        + $"    <key>Label</key>\n    <string>{Label}</string>\n"
        + $"    <key>ProgramArguments</key>\n    <array>\n        <string>{Clean(launcherPath)}</string>\n        <string>--minimized</string>\n    </array>\n"
        + "    <key>RunAtLoad</key>\n    <true/>\n</dict>\n</plist>\n";

    private static string Clean(string path) => SecurityElement.Escape(new string([.. path.Where(character => !char.IsControl(character))])) ?? string.Empty;
}
