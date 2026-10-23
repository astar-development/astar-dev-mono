using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>Builds the platform command that shows a <see cref="DesktopNotification"/>.</summary>
public static class NotificationCommandBuilder
{
    private const string ApplicationName = "AStar OneDrive Sync";

    /// <summary>Returns the command for the platform, or None when the platform has no supported mechanism.</summary>
    public static Option<ProcessCommand> Build(DesktopNotification notification, DesktopPlatform platform) => platform switch
    {
        DesktopPlatform.Linux => Option.Some(ProcessCommandFactory.Create("notify-send", $"--app-name={ApplicationName}", "--", notification.Title, notification.Body)),
        DesktopPlatform.MacOs => Option.Some(ProcessCommandFactory.Create("osascript", "-e", $"display notification \"{EscapeAppleScript(notification.Body)}\" with title \"{EscapeAppleScript(notification.Title)}\"")),
        _ => Option.None<ProcessCommand>()
    };

    private static string EscapeAppleScript(string text) => text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
