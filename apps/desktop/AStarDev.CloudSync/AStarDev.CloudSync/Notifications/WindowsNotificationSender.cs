using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Notifications;

/// <summary>Shows a Windows toast by running PowerShell with a fixed script (#1410).</summary>
[ExcludeFromCodeCoverage(Justification = "Starts a real process; the command it runs is tested in WindowsNotificationCommandBuilder.")]
internal sealed class WindowsNotificationSender : ProcessNotificationSender
{
    /// <inheritdoc />
    protected override Option<NotificationCommand> CommandFor(DesktopNotification notification) =>
        WindowsNotificationCommandBuilder.FindProgram(Environment.SystemDirectory, File.Exists).Match(path => Option.Some(WindowsNotificationCommandBuilder.Build(path, notification)), () => Option.None<NotificationCommand>());
}
