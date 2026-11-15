using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.Common;

/// <summary>Runs an operation inside <c>Try</c> and applies its result or its exception on the caller's context, which is the UI thread for a view model. <c>MatchAsync</c> continues with <c>ConfigureAwait(false)</c>, so the lambdas it is given run on a pool thread; this keeps them on the UI thread (#1624, #1658).</summary>
internal static class UiResumingTry
{
    /// <summary>Runs <paramref name="operation"/> and applies one of the two lambdas to what happened. Cancellation, including an operation that throws <see cref="OperationCanceledException"/> by itself, still throws.</summary>
    /// <typeparam name="T">What the operation returns.</typeparam>
    /// <typeparam name="TResult">What the lambdas return.</typeparam>
    /// <param name="operation">The work.</param>
    /// <param name="onResult">Applied to the operation's result, on the caller's context.</param>
    /// <param name="onThrown">Applied to the exception the operation threw, on the caller's context.</param>
    /// <param name="cancellationToken">The caller's token.</param>
    public static async Task<TResult> RunAsync<T, TResult>(Func<Task<T>> operation, Func<T, TResult> onResult, Func<Exception, TResult> onThrown, CancellationToken cancellationToken)
    {
        var attempt = await SettledAsync(operation, cancellationToken);

        return (await attempt).Match(onResult, onThrown);
    }

    /// <summary>As <see cref="RunAsync{T, TResult}"/>, except that an operation cancelled while the caller's token was not cancelled (an HTTP client timeout, for example) arrives in <paramref name="onThrown"/> as a <see cref="TimeoutException"/> instead of throwing.</summary>
    /// <typeparam name="T">What the operation returns.</typeparam>
    /// <typeparam name="TResult">What the lambdas return.</typeparam>
    /// <param name="operation">The work.</param>
    /// <param name="onResult">Applied to the operation's result, on the caller's context.</param>
    /// <param name="onThrown">Applied to the exception the operation threw, on the caller's context.</param>
    /// <param name="cancellationToken">The caller's token; cancelling it still throws.</param>
    public static async Task<TResult> RunTreatingTimeoutAsFailureAsync<T, TResult>(Func<Task<T>> operation, Func<T, TResult> onResult, Func<Exception, TResult> onThrown, CancellationToken cancellationToken)
    {
        var attempt = await SettledAsync(operation, cancellationToken);
        Exceptional<T> settled = attempt.IsCanceled && !cancellationToken.IsCancellationRequested ? new Failure<T>(new TimeoutException()) : await attempt;

        return settled.Match(onResult, onThrown);
    }

    private static async Task<Task<Exceptional<T>>> SettledAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        var attempt = Try.RunAsync(operation, cancellationToken);
        await ((Task)attempt).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);

        return attempt;
    }
}
