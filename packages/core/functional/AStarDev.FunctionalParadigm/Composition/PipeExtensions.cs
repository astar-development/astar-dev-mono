namespace AStarDev.FunctionalParadigm.Composition;

/// <summary>Chaining helpers for plain values and functions, enabling pipeline-style composition without intermediate locals.</summary>
/// <remarks>
///     Deliberately kept in a dedicated namespace rather than <see cref="FunctionalParadigm" />:
///     <c>Tap</c> here is generic over any value type, which would otherwise collide with
///     (and win overload resolution against) the more specific <c>Tap</c> overloads on
///     <c>Result&lt;TResult,TError&gt;</c>, <c>Option&lt;T&gt;</c> and <c>Exceptional&lt;T&gt;</c> for any
///     single-argument call. Callers opt in explicitly with <c>using AStarDev.FunctionalParadigm.Composition;</c>.
/// </remarks>
public static class PipeExtensions
{
    extension<TIn>(TIn value)
    {
        /// <summary>Pipes the value through the specified function, returning the function's result.</summary>
        public TOut Pipe<TOut>(Func<TIn, TOut> fn) => fn(value);

        /// <summary>Asynchronously pipes the value through the specified function, returning the function's result.</summary>
        public async Task<TOut> PipeAsync<TOut>(Func<TIn, Task<TOut>> fn) => await fn(value).ConfigureAwait(false);

        /// <summary>Executes a side-effect action on the value, and returns the original value unchanged.</summary>
        public TIn Tap(Action<TIn> sideEffect)
        {
            sideEffect(value);

            return value;
        }
    }

    extension<TIn, TMid>(Func<TIn, TMid> first)
    {
        /// <summary>Composes two functions into one, running <paramref name="first" /> then <paramref name="second" />.</summary>
        public Func<TIn, TOut> Compose<TOut>(Func<TMid, TOut> second) => input => second(first(input));
    }
}
