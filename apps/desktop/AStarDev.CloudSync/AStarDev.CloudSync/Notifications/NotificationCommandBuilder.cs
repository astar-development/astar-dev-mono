using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Notifications;

/// <summary>Builds the argument list for <c>notify-send</c> (#1410). The title and the body are separate list items and come after <c>--</c>, so text that looks like an option or like shell syntax is only ever text. No shell is involved.</summary>
public static class NotificationCommandBuilder
{
    private static readonly string[] ProgramLocations = ["/usr/bin/notify-send", "/usr/local/bin/notify-send", "/bin/notify-send"];

    /// <summary>Finds <c>notify-send</c> at a fixed, absolute location, so a program of the same name earlier on <c>PATH</c> is never run.</summary>
    /// <param name="exists">Tells whether a file exists at a path.</param>
    /// <returns>The first location that exists, or nothing when the program is not installed.</returns>
    public static Option<string> FindProgram(Func<string, bool> exists) => ProgramLocations.FirstOrNone(exists);

    /// <summary>Builds the arguments.</summary>
    /// <param name="notification">The title and body.</param>
    public static IReadOnlyList<string> Arguments(DesktopNotification notification) => ["--app-name=CloudSync", "--", notification.Title, notification.Body];
}
