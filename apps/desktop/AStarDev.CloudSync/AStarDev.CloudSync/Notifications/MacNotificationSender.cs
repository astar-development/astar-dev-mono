using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Notifications;

/// <summary>Shows a macOS notification by running <c>osascript</c> with a fixed script (#1410).</summary>
[ExcludeFromCodeCoverage(Justification = "Starts a real process; the command it runs is tested in MacNotificationCommandBuilder.")]
internal sealed class MacNotificationSender : ProcessNotificationSender
{
    /// <inheritdoc />
    protected override Option<NotificationCommand> CommandFor(DesktopNotification notification) =>
        MacNotificationCommandBuilder.FindProgram(File.Exists).Match(path => Option.Some(MacNotificationCommandBuilder.Build(path, notification)), () => Option.None<NotificationCommand>());
}
