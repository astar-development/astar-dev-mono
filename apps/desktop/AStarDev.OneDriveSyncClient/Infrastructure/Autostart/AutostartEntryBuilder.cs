namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <summary>Builds the per-platform content that registers the application to launch at OS startup.</summary>
public static class AutostartEntryBuilder
{
    /// <summary>Builds a freedesktop autostart entry that starts the application after a delay where the desktop supports it.</summary>
    public static string BuildDesktopEntry(string executablePath, int delaySeconds) => throw new NotImplementedException(string.Join(',', executablePath, delaySeconds));

    /// <summary>Builds a macOS LaunchAgent property list that starts the application at login.</summary>
    public static string BuildLaunchAgent(string label, string executablePath) => throw new NotImplementedException(string.Join(',', label, executablePath));

    /// <summary>Builds the command stored in the Windows Run key.</summary>
    public static string BuildWindowsCommand(string executablePath) => throw new NotImplementedException(executablePath);
}
