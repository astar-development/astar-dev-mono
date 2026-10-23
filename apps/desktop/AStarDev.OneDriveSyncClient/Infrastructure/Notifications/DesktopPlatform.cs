namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>Base type for the desktop platforms a notification can be shown on.</summary>
public abstract record DesktopPlatform
{
    /// <summary>Linux, using the freedesktop notification service.</summary>
    public sealed record Linux : DesktopPlatform;

    /// <summary>macOS, using AppleScript.</summary>
    public sealed record MacOs : DesktopPlatform;

    /// <summary>Windows, which has no supported notification mechanism yet.</summary>
    public sealed record Windows : DesktopPlatform;
}
