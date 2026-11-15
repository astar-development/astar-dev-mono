using System.Text;
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

    private const char Backslash = (char)92;

    private const string ScriptText = """
        $ErrorActionPreference = 'Stop'
        [void][Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime]
        $template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)
        $text = $template.GetElementsByTagName('text')
        [void]$text.Item(0).AppendChild($template.CreateTextNode($env:CLOUDSYNC_NOTIFY_TITLE))
        [void]$text.Item(1).AppendChild($template.CreateTextNode($env:CLOUDSYNC_NOTIFY_BODY))
        $toast = [Windows.UI.Notifications.ToastNotification]::new($template)
        [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}\WindowsPowerShell\v1.0\powershell.exe').Show($toast)
        """;

    /// <summary>The fixed script, for tests.</summary>
    public static string Script => ScriptText;

    /// <summary>Finds Windows PowerShell under the system folder, so a program of the same name earlier on <c>PATH</c> is never run.</summary>
    /// <param name="systemDirectory">The Windows system folder.</param>
    /// <param name="exists">Tells whether a file exists at a path.</param>
    public static Option<string> FindProgram(string systemDirectory, Func<string, bool> exists) => new[] { $@"{systemDirectory.TrimEnd(Backslash)}\WindowsPowerShell\v1.0\powershell.exe" }.FirstOrNone(exists);

    /// <summary>Builds the command.</summary>
    /// <param name="program">The absolute path of PowerShell.</param>
    /// <param name="notification">The title and body.</param>
    public static NotificationCommand Build(string program, DesktopNotification notification) => NotificationCommandFactory.Create(
        program,
        ["-NoProfile", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(ScriptText))],
        new Dictionary<string, string> { [TitleVariable] = notification.Title, [BodyVariable] = notification.Body });
}
