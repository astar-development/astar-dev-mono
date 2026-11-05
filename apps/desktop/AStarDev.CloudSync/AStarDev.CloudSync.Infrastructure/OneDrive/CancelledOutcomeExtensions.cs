using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

internal static class CancelledOutcomeExtensions
{
    public static async Task<Result<T, AccountAddOutcome>> OrCancelledAsync<T>(this Task<Result<T, AccountAddOutcome>> step, Action onTimedOut, CancellationToken cancellationToken)
    {
        await ((Task)step).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (!step.IsCanceled)
            return await step.ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
            return Result.Failure<T, AccountAddOutcome>(AccountAddOutcomeFactory.CreateCancelled());

        onTimedOut();

        return Result.Failure<T, AccountAddOutcome>(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork()));
    }
}
