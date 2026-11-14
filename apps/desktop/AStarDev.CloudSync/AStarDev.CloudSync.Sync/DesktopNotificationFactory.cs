namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DesktopNotification"/> values.</summary>
public static class DesktopNotificationFactory
{
    /// <summary>Creates a notification. A missing title or body becomes empty text.</summary>
    /// <param name="title">The headline.</param>
    /// <param name="body">The text under the headline.</param>
    public static DesktopNotification Create(string? title, string? body) => new(title ?? string.Empty, body ?? string.Empty);
}
