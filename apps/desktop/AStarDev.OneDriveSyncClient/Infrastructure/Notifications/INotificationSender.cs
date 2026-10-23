namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>Shows an OS-level notification.</summary>
public interface INotificationSender
{
    /// <summary>Shows the notification. Failure to show it never throws.</summary>
    Task SendAsync(DesktopNotification notification, CancellationToken cancellationToken = default);
}
