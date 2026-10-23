namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>Creates <see cref="DesktopNotification"/> instances.</summary>
public static class DesktopNotificationFactory
{
    /// <summary>Creates a notification, normalising missing text to empty strings.</summary>
    public static DesktopNotification Create(string? title, string? body) => new(title ?? string.Empty, body ?? string.Empty);
}
