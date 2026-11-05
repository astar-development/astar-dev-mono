using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

internal static class CancelledOutcomeExtensions
{
    public static async Task<Result<T, AccountAddOutcome>> OrCancelledAsync<T>(this Task<Result<T, AccountAddOutcome>> step)
    {
        await ((Task)step).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return step.IsCanceled ? Result.Failure<T, AccountAddOutcome>(AccountAddOutcomeFactory.CreateCancelled()) : await step.ConfigureAwait(false);
    }
}
