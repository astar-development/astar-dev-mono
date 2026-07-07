namespace AStar.Dev.FunctionalParadigm.Composition;

/// <summary>
///     Chaining helpers for plain values and functions, enabling pipeline-style composition without
///     intermediate locals.
/// </summary>
/// <remarks>
///     Deliberately kept in a dedicated namespace rather than <see cref="AStar.Dev.FunctionalParadigm" />:
///     <see cref="Tap{T}" /> is generic over any <typeparamref name="T" />, which would otherwise collide with
///     (and win overload resolution against) the more specific <c>Tap</c> overloads on
///     <c>Result&lt;TResult,TError&gt;</c>, <c>Option&lt;T&gt;</c> and <c>Exceptional&lt;T&gt;</c> for any
///     single-argument call. Callers opt in explicitly with <c>using AStar.Dev.FunctionalParadigm.Composition;</c>.
/// </remarks>
public static class FunctionExtensions
{
    /// <summary>
    ///     Pipes the value through the specified function, returning the function's result.
    /// </summary>
    public static TOut Pipe<TIn, TOut>(this TIn value, Func<TIn, TOut> fn) => fn(value);

    /// <summary>
    ///     Asynchronously pipes the value through the specified function, returning the function's result.
    /// </summary>
    public static async Task<TOut> PipeAsync<TIn, TOut>(this TIn value, Func<TIn, Task<TOut>> fn) => await fn(value).ConfigureAwait(false);

    /// <summary>
    ///     Executes a side-effect action on the value, and returns the original value unchanged.
    /// </summary>
    public static T Tap<T>(this T value, Action<T> sideEffect)
    {
        sideEffect(value);

        return value;
    }

    /// <summary>
    ///     Composes two functions into one, running <paramref name="first" /> then <paramref name="second" />.
    /// </summary>
    public static Func<TIn, TOut> Compose<TIn, TMid, TOut>(this Func<TIn, TMid> first, Func<TMid, TOut> second) => input => second(first(input));
}
