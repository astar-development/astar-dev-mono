using AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <summary>Creates <see cref="AutostartContext"/> instances.</summary>
public static class AutostartContextFactory
{
    /// <summary>Creates a context, normalising missing values to empty strings.</summary>
    public static AutostartContext Create(DesktopPlatform platform, string? executablePath, string? homeDirectory, string? xdgConfigHome) => new(platform, executablePath ?? string.Empty, homeDirectory ?? string.Empty, xdgConfigHome ?? string.Empty);
}
