using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public sealed class ProcessNotificationSender(ILogger<ProcessNotificationSender> logger) : INotificationSender
{
    /// <inheritdoc />
    public async Task SendAsync(DesktopNotification notification, CancellationToken cancellationToken = default)
    {
        var command = NotificationCommandBuilder.Build(notification, DesktopPlatformFactory.CreateCurrent());

        await command.Match(command => RunAsync(command, cancellationToken), () => Task.CompletedTask).ConfigureAwait(false);
    }

    private async Task RunAsync(ProcessCommand command, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(command.FileName) { UseShellExecute = false, CreateNoWindow = true };

        foreach (string argument in command.Arguments)
            startInfo.ArgumentList.Add(argument);

        try
        {
            using var process = Process.Start(startInfo);

            if (process is not null)
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Win32Exception ex)
        {
            OneDriveSyncClientMessages.NotificationFailed(logger, command.FileName, ex.Message, ex);
        }
    }
}
