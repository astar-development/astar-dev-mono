using System.Security;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <summary>Builds the per-platform content that registers the application to launch at OS startup.</summary>
public static class AutostartEntryBuilder
{
    private const string ApplicationName = "AStar OneDrive Sync";

    /// <summary>Builds a freedesktop autostart entry that starts the application after a delay where the desktop supports it.</summary>
    public static string BuildDesktopEntry(string executablePath, int delaySeconds) => string.Join('\n',
        "[Desktop Entry]",
        "Type=Application",
        $"Name={ApplicationName}",
        $"Exec=\"{EscapeDesktopExec(executablePath)}\"",
        "Terminal=false",
        "X-GNOME-Autostart-enabled=true",
        $"X-GNOME-Autostart-Delay={delaySeconds}") + "\n";

    /// <summary>Builds a macOS LaunchAgent property list that starts the application at login.</summary>
    public static string BuildLaunchAgent(string label, string executablePath) => string.Join('\n',
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
        "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">",
        "<plist version=\"1.0\">",
        "<dict>",
        "  <key>Label</key>",
        $"  <string>{SecurityElement.Escape(label)}</string>",
        "  <key>ProgramArguments</key>",
        "  <array>",
        $"    <string>{SecurityElement.Escape(executablePath)}</string>",
        "  </array>",
        "  <key>RunAtLoad</key>",
        "  <true/>",
        "</dict>",
        "</plist>") + "\n";

    /// <summary>Builds the command stored in the Windows Run key.</summary>
    public static string BuildWindowsCommand(string executablePath) => $"\"{executablePath}\"";

    private static string EscapeDesktopExec(string path) => path
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("`", "\\`", StringComparison.Ordinal)
        .Replace("$", "\\$", StringComparison.Ordinal);
}
