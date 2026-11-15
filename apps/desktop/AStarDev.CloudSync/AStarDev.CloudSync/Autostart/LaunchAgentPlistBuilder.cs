namespace AStarDev.CloudSync.Autostart;

/// <summary>Builds the text of the macOS LaunchAgent property list that starts the app at login (#1410).</summary>
public static class LaunchAgentPlistBuilder
{
    /// <summary>The label that names the agent.</summary>
    public const string Label = "dev.astar.cloudsync";

    /// <summary>Builds the plist that starts <paramref name="launcherPath"/> hidden in the tray when the user logs in. The path is XML-escaped and control characters in it are dropped, so a path can never add a key or an argument of its own.</summary>
    /// <param name="launcherPath">The full path of the program.</param>
    public static string Build(string launcherPath) => throw new NotImplementedException($"{nameof(launcherPath)}");
}
