namespace AStarDev.CloudSync.Autostart;

/// <summary>Builds the command line stored in the Windows <c>Run</c> key (#1410).</summary>
public static class RunCommandBuilder
{
    /// <summary>Builds the command that starts <paramref name="launcherPath"/> hidden in the tray. The path is quoted, and quotes and line breaks in it are dropped, so a path can never add an argument of its own.</summary>
    /// <param name="launcherPath">The full path of the program.</param>
    public static string Build(string launcherPath) => throw new NotImplementedException($"{nameof(launcherPath)}");
}
