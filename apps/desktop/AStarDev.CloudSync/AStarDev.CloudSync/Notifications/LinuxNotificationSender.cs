using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Notifications;

/// <summary>Shows a notification by running <c>notify-send</c> with an argument list (#1410). A missing program or a failed run is a <see cref="NotificationFailure.SenderFailed"/>, never an exception.</summary>
[ExcludeFromCodeCoverage(Justification = "Starts a real process; the arguments it passes are tested in NotificationCommandBuilder.")]
internal sealed class LinuxNotificationSender : INotificationSender
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public async Task<Result<Unit, NotificationFailure>> SendAsync(DesktopNotification notification, CancellationToken cancellationToken)
    {
        var program = NotificationCommandBuilder.FindProgram(File.Exists);
        var run = Try.RunAsync(() => program.Match(path => RunAsync(StartInfoFor(path, notification), cancellationToken), () => Task.FromResult(false)), cancellationToken);
        await ((Task)run).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return run.IsCompletedSuccessfully && run.Result.Match(done => done, _ => false)
            ? Result.Success<Unit, NotificationFailure>(Unit.Instance)
            : Result.Failure<Unit, NotificationFailure>(NotificationFailureFactory.CreateSenderFailed());
    }

    private static ProcessStartInfo StartInfoFor(string program, DesktopNotification notification)
    {
        var start = new ProcessStartInfo(program) { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in NotificationCommandBuilder.Arguments(notification))
            start.ArgumentList.Add(argument);

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
