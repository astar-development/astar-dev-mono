using AStarDev.WallpaperScraper.Operations;
using System.Diagnostics.CodeAnalysis;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>Runs a user-initiated operation as the single active operation, reporting cancellation and every failure to the user instead of throwing, so nothing can escape the <c>async void</c> handler it is called from.</summary>
/// <param name="operationCoordinator">Tracks whether an operation is running and lets it be cancelled.</param>
/// <param name="status">Where cancellation and failure messages are reported.</param>
public sealed class UserOperationRunner(OperationCoordinator operationCoordinator, StatusReporter status)
{
    /// <summary>Runs <paramref name="operation"/> unless another operation is already running.</summary>
    /// <param name="cancelledMessage">Reported when the operation is cancelled.</param>
    /// <param name="failureMessage">Reported, followed by the exception's message and its causes, when the operation fails for any reason other than cancellation.</param>
    /// <param name="operation">The operation, given the token that <see cref="Cancel"/> cancels.</param>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Called from async void UI handlers: any exception escaping them would crash the application, so every failure is reported in the status instead.")]
    public async Task RunAsync(string cancelledMessage, string failureMessage, Func<CancellationToken, Task> operation)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

        try
        {
            await operation(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            status.Append(cancelledMessage);
        }
        catch (Exception exception)
        {
            status.Error(failureMessage, exception);
        }
        finally
        {
            operationCoordinator.Complete();
        }
    }

    /// <summary>Runs <paramref name="operation"/> reporting any failure to the user instead of throwing. Unlike <see cref="RunAsync"/> it does not take the single-operation slot, so it suits modal work such as a dialog.</summary>
    /// <param name="failureMessage">Reported, followed by the exception's message and its causes, when the operation fails.</param>
    /// <param name="operation">The operation to run.</param>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Called from async void UI handlers: any exception escaping them would crash the application, so every failure is reported in the status instead.")]
    public async Task ReportFailuresAsync(string failureMessage, Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception exception)
        {
            status.Error(failureMessage, exception);
        }
    }

    /// <summary>Cancels the running operation, if any.</summary>
    public void Cancel() => operationCoordinator.Cancel();
}
