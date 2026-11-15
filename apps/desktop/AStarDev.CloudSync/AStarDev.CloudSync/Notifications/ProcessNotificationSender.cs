using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Notifications;

/// <summary>Shows a notification by running a program with an argument list (#1410). A missing program or a failed run is a <see cref="NotificationFailure.SenderFailed"/>, never an exception.</summary>
[ExcludeFromCodeCoverage(Justification = "Starts a real process; the commands it runs are tested in the command builders.")]
internal abstract class ProcessNotificationSender : INotificationSender
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public async Task<Result<Unit, NotificationFailure>> SendAsync(DesktopNotification notification, CancellationToken cancellationToken)
    {
        var run = Try.RunAsync(() => CommandFor(notification).Match(command => RunAsync(StartInfoFor(command), cancellationToken), () => Task.FromResult(false)), cancellationToken);
        await ((Task)run).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return run.IsCompletedSuccessfully && run.Result.Match(done => done, _ => false)
            ? Result.Success<Unit, NotificationFailure>(Unit.Instance)
            : Result.Failure<Unit, NotificationFailure>(NotificationFailureFactory.CreateSenderFailed());
    }

    /// <summary>Builds the command for one notification, or nothing when the program is not installed.</summary>
    /// <param name="notification">The title and body.</param>
    protected abstract Option<NotificationCommand> CommandFor(DesktopNotification notification);

    private static ProcessStartInfo StartInfoFor(NotificationCommand command)
    {
        var start = new ProcessStartInfo(command.Program) { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in command.Arguments)
            start.ArgumentList.Add(argument);

        foreach (var variable in command.Environment)
            start.Environment[variable.Key] = variable.Value;

        return start;
    }

    private static async Task<bool> RunAsync(ProcessStartInfo start, CancellationToken cancellationToken)
    {
        using var process = Process.Start(start);
        if (process is null)
            return false;

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(Patience);
        await process.WaitForExitAsync(limit.Token).ConfigureAwait(false);

        return process.ExitCode == 0;
    }
}
