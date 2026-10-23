using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>Builds the platform command that shows a <see cref="DesktopNotification"/>.</summary>
public static class NotificationCommandBuilder
{
    /// <summary>Returns the command for the platform, or None when the platform has no supported mechanism.</summary>
    public static Option<ProcessCommand> Build(DesktopNotification notification, DesktopPlatform platform) => throw new NotImplementedException(string.Join(',', notification, platform));
}
