using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

internal static class CancelledOutcomeExtensions
{
    public static async Task<Result<T, AccountAddOutcome>> OrCancelledAsync<T>(this Task<Result<T, AccountAddOutcome>> step, CancellationToken cancellationToken)
    {
        await ((Task)step).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return step.IsCanceled
            ? Result.Failure<T, AccountAddOutcome>(CancelledOutcome(cancellationToken))
            : await step.ConfigureAwait(false);
    }

    private static AccountAddOutcome CancelledOutcome(CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested ? AccountAddOutcomeFactory.CreateCancelled() : AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork());
}
