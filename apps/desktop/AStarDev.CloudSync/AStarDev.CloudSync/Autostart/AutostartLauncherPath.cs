using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Autostart;

/// <summary>Chooses the program the start at login entry runs (#1787).</summary>
public static class AutostartLauncherPath
{
    /// <summary>The path the entry runs: the <c>.AppImage</c> file when the app runs from one, because the process path is then a temporary mount that is gone after the app exits; otherwise the process path. Nothing for a build that is not installed or has no path.</summary>
    /// <param name="isInstalled">Whether this copy was installed by Velopack.</param>
    /// <param name="processPath">The path of the running process.</param>
    /// <param name="appImagePath">The value of the <c>APPIMAGE</c> environment variable, if set.</param>
    public static Option<string> Resolve(bool isInstalled, string? processPath, string? appImagePath)
    {
        if (!isInstalled)
            return Option.None<string>();

        if (!string.IsNullOrWhiteSpace(appImagePath))
            return Option.Some(appImagePath);

        return processPath is { Length: > 0 } ? Option.Some(processPath) : Option.None<string>();
    }
}
