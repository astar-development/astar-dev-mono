namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>An OS-level notification to show to the user.</summary>
/// <param name="Title">The notification title.</param>
/// <param name="Body">The notification body text.</param>
public sealed record DesktopNotification(string Title, string Body);
