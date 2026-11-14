using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Shows a notification with the operating system's own mechanism (#1410). There is one sender per platform.</summary>
public interface INotificationSender
{
    /// <summary>Shows the notification.</summary>
    /// <param name="notification">The title and body.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, or why nothing was shown. A failure never fails a sync pass.</returns>
    Task<Result<Unit, NotificationFailure>> SendAsync(DesktopNotification notification, CancellationToken cancellationToken);
}
