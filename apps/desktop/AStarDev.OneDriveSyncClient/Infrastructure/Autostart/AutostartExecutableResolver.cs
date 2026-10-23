namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <summary>Chooses the executable path that startup registration should launch.</summary>
public static class AutostartExecutableResolver
{
    /// <summary>Returns the AppImage path when running from an AppImage, otherwise the process path.</summary>
    public static string Resolve(string? processPath, string? appImagePath) => string.IsNullOrWhiteSpace(appImagePath) ? processPath ?? string.Empty : appImagePath;
}
