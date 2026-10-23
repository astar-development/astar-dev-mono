namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>Creates <see cref="DesktopPlatform"/> instances.</summary>
public static class DesktopPlatformFactory
{
    /// <summary>Creates the Linux platform.</summary>
    public static DesktopPlatform CreateLinux() => new DesktopPlatform.Linux();

    /// <summary>Creates the macOS platform.</summary>
    public static DesktopPlatform CreateMacOs() => new DesktopPlatform.MacOs();

    /// <summary>Creates the Windows platform.</summary>
    public static DesktopPlatform CreateWindows() => new DesktopPlatform.Windows();

    /// <summary>Detects the platform the application is running on, treating anything that is not Windows or macOS as Linux.</summary>
    public static DesktopPlatform CreateCurrent() => OperatingSystem.IsWindows() ? CreateWindows() : OperatingSystem.IsMacOS() ? CreateMacOs() : CreateLinux();
}
