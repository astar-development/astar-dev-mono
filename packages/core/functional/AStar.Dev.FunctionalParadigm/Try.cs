namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Bridges throwing operations into <see cref="Exceptional{T}" />, capturing thrown exceptions into a
///     <see cref="Failure{T}" /> instead of letting them propagate.
/// </summary>
/// <remarks>
///     <see cref="OperationCanceledException" /> (including <see cref="TaskCanceledException" />) is never
///     captured — it always rethrows, so <see cref="CancellationToken.ThrowIfCancellationRequested" /> semantics
///     survive.
/// </remarks>
public static class Try
{
    /// <summary>
    ///     Runs the specified operation, capturing any thrown exception (other than
    ///     <see cref="OperationCanceledException" />) into a <see cref="Failure{T}" />.
    /// </summary>
    public static Exceptional<T> Run<T>(Func<T> operation)
    {
#pragma warning disable CA1031 // Do not catch general exception types - this is the point of this method
        try
        {
            return new Success<T>(operation());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new Failure<T>(exception);
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    /// <summary>
    ///     Runs the specified operation, throwing immediately if <paramref name="cancellationToken" /> is
    ///     cancelled, and otherwise capturing any thrown exception (other than
    ///     <see cref="OperationCanceledException" />) into a <see cref="Failure{T}" />.
    /// </summary>
    public static Exceptional<T> Run<T>(Func<T> operation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Run(operation);
    }

    /// <summary>
    ///     Runs the specified asynchronous operation, capturing any thrown exception (other than
    ///     <see cref="OperationCanceledException" />) into a <see cref="Failure{T}" />.
    /// </summary>
    public static async Task<Exceptional<T>> RunAsync<T>(Func<Task<T>> operation)
    {
#pragma warning disable CA1031 // Do not catch general exception types - this is the point of this method
        try
        {
            var value = await operation().ConfigureAwait(false);

            return new Success<T>(value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new Failure<T>(exception);
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    /// <summary>
    ///     Runs the specified asynchronous operation, throwing immediately if <paramref name="cancellationToken" />
    ///     is cancelled, and otherwise capturing any thrown exception (other than
    ///     <see cref="OperationCanceledException" />) into a <see cref="Failure{T}" />.
    /// </summary>
    public static async Task<Exceptional<T>> RunAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await RunAsync(operation).ConfigureAwait(false);
    }

    /// <summary>
    ///    Runs the specified operation, executing <paramref name="finallyAction" /> after the operation completes,
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="result">The result of the operation.</param>
    /// <param name="finallyAction">The action to execute after the operation completes.</param>
    /// <returns>The original result.</returns>
    public static Exceptional<T> Ensure<T>(this Exceptional<T> result, Action finallyAction)
    {
        finallyAction();

        return result;
    }
}
