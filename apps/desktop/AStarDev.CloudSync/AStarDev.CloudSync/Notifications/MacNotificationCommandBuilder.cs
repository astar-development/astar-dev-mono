using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Notifications;

/// <summary>Builds the <c>osascript</c> command that shows a macOS notification (#1410). The script is a fixed constant that reads the title and body from its own arguments, so the text is never part of the script.</summary>
public static class MacNotificationCommandBuilder
{
    private const string Location = "/usr/bin/osascript";

    /// <summary>Finds <c>osascript</c> at its fixed location, so a program of the same name earlier on <c>PATH</c> is never run.</summary>
    /// <param name="exists">Tells whether a file exists at a path.</param>
    public static Option<string> FindProgram(Func<string, bool> exists) => new[] { Location }.FirstOrNone(exists);

    /// <summary>Builds the command.</summary>
    /// <param name="program">The absolute path of <c>osascript</c>.</param>
    /// <param name="notification">The title and body.</param>
    public static NotificationCommand Build(string program, DesktopNotification notification) => NotificationCommandFactory.Create(
        program,
        ["-e", "on run argv", "-e", "display notification (item 2 of argv) with title (item 1 of argv)", "-e", "end run", "--", notification.Title, notification.Body]);
}
