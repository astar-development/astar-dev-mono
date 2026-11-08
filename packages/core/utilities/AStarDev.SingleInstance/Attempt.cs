using AStarDev.FunctionalParadigm;

namespace AStarDev.SingleInstance;

/// <summary>Runs socket work that may fail or be cancelled and turns any outcome into an option, so a timeout never escapes as an exception.</summary>
internal static class Attempt
{
    /// <summary>Runs the operation and returns its value, or none when it threw or was cancelled. The caller checks its own token to tell a user cancel from a timeout.</summary>
    /// <param name="operation">The work to run.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    public static async Task<Option<T>> RunAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        var running = Task.Run(operation, cancellationToken);
        await ((Task)running).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return running.IsCompletedSuccessfully ? Option.Some(await running.ConfigureAwait(false)) : Option.None<T>();
    }
}
