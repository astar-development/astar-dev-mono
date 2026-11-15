using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Notifications;

/// <summary>Shows a notification by running <c>notify-send</c> (#1410).</summary>
[ExcludeFromCodeCoverage(Justification = "Starts a real process; the arguments it passes are tested in NotificationCommandBuilder.")]
internal sealed class LinuxNotificationSender : ProcessNotificationSender
{
    /// <inheritdoc />
    protected override Option<NotificationCommand> CommandFor(DesktopNotification notification) =>
        NotificationCommandBuilder.FindProgram(File.Exists).Match(path => Option.Some(NotificationCommandFactory.Create(path, NotificationCommandBuilder.Arguments(notification))), () => Option.None<NotificationCommand>());
}
