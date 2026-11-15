using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Notifications;

/// <summary>Builds the PowerShell command that shows a Windows toast (#1410). The script is a fixed constant; the title and body reach it only through environment variables and are added to the toast as text nodes, so they are never parsed as script or markup.</summary>
public static class WindowsNotificationCommandBuilder
{
    /// <summary>The variable that holds the title.</summary>
    public const string TitleVariable = "CLOUDSYNC_NOTIFY_TITLE";

    /// <summary>The variable that holds the body.</summary>
    public const string BodyVariable = "CLOUDSYNC_NOTIFY_BODY";

    /// <summary>Finds Windows PowerShell under the system folder, so a program of the same name earlier on <c>PATH</c> is never run.</summary>
    /// <param name="systemDirectory">The Windows system folder.</param>
    /// <param name="exists">Tells whether a file exists at a path.</param>
    public static Option<string> FindProgram(string systemDirectory, Func<string, bool> exists) => throw new NotImplementedException($"{systemDirectory} {exists}");

    /// <summary>Builds the command.</summary>
    /// <param name="program">The absolute path of PowerShell.</param>
    /// <param name="notification">The title and body.</param>
    public static NotificationCommand Build(string program, DesktopNotification notification) => throw new NotImplementedException($"{program} {notification}");

    /// <summary>The fixed script, for tests.</summary>
    public static string Script => throw new NotImplementedException();
}
