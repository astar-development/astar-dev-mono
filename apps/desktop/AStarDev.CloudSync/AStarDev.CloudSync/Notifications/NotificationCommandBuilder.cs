using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Notifications;

/// <summary>Builds the argument list for <c>notify-send</c> (#1410). The title and the body are separate list items and come after <c>--</c>, so text that looks like an option or like shell syntax is only ever text. No shell is involved.</summary>
public static class NotificationCommandBuilder
{
    /// <summary>Builds the arguments.</summary>
    /// <param name="notification">The title and body.</param>
    public static IReadOnlyList<string> Arguments(DesktopNotification notification) => ["--app-name=CloudSync", "--", notification.Title, notification.Body];
}
