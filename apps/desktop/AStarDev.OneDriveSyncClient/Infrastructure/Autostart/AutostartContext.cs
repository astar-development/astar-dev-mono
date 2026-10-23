using AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <summary>The environment details needed to register the application to launch at OS startup.</summary>
/// <param name="Platform">The desktop platform.</param>
/// <param name="ExecutablePath">The full path of the running executable.</param>
/// <param name="HomeDirectory">The user's home directory.</param>
/// <param name="XdgConfigHome">The XDG config home override, or empty when unset.</param>
public sealed record AutostartContext(DesktopPlatform Platform, string ExecutablePath, string HomeDirectory, string XdgConfigHome);
