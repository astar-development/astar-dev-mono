namespace AStarDev.CloudSync.Autostart;

/// <summary>Builds the text of the XDG autostart entry (#1410). The program path is quoted and escaped for the <c>Exec</c> key, and a line break in it is dropped, so a path can never add a key of its own.</summary>
public static class DesktopEntryBuilder
{
    /// <summary>Builds the entry that starts <paramref name="launcherPath"/> hidden in the tray, 30 seconds after login.</summary>
    /// <param name="launcherPath">The full path of the program.</param>
    public static string Build(string launcherPath) =>
        $"[Desktop Entry]\nType=Application\nName=AStar Dev CloudSync\nExec=\"{Escape(launcherPath)}\" --minimized\nX-GNOME-Autostart-Delay=30\nTerminal=false\n";

    private static string Escape(string path) => path
        .Replace("\r", string.Empty, StringComparison.Ordinal)
        .Replace("\n", string.Empty, StringComparison.Ordinal)
        .Replace("\\", "\\\\\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\\\"", StringComparison.Ordinal)
        .Replace("`", "\\\\`", StringComparison.Ordinal)
        .Replace("$", "\\\\$", StringComparison.Ordinal)
        .Replace("%", "%%", StringComparison.Ordinal);
}
