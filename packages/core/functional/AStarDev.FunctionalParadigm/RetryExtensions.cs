namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Retry helpers for <see cref="Result{TResult,TError}" />-returning operations.
/// </summary>
public static class RetryExtensions
{
    /// <summary>
    ///     Runs <paramref name="operation" /> once. If it fails, runs <paramref name="onRetry" /> and then runs
    ///     <paramref name="operation" /> a second and final time, returning whichever attempt's result is current
    ///     at that point (the second attempt's result when both fail).
    /// </summary>
    /// <param name="operation">The operation to run, at most twice.</param>
    /// <param name="onRetry">The side effect (typically a delay) to run between the first and second attempts.</param>
    public static async Task<Result<TResult, TError>> RetryOnceAsync<TResult, TError>(Func<Task<Result<TResult, TError>>> operation, Func<Task> onRetry)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(onRetry);

        var firstAttempt = await operation().ConfigureAwait(false);

        return await firstAttempt.MatchAsync(
            value => Task.FromResult(Result.Success<TResult, TError>(value)),
            async _ =>
            {
                await onRetry().ConfigureAwait(false);

                return await operation().ConfigureAwait(false);
            }).ConfigureAwait(false);
    }
}
