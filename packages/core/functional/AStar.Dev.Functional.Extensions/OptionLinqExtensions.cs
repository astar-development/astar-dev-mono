namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     LINQ-style query support for <see cref="Option{T}" />.
/// </summary>
public static class OptionLinqExtensions
{
    /// <summary>
    ///     Projects the value of a <see cref="Option{T}" /> using the specified function.
    /// </summary>
    public static Option<TResult> Select<T, TResult>(this Option<T> option, Func<T, TResult> selector) => option.Map(selector);

    /// <summary>
    ///     Projects and flattens nested <see cref="Option{T}" /> structures using a LINQ-style binding function.
    /// </summary>
    public static Option<TResult> SelectMany<T, TIntermediate, TResult>(this Option<T> option, Func<T, Option<TIntermediate>> bind, Func<T, TIntermediate, TResult> project) =>
        option.Bind(x => bind(x).Map(y => project(x, y)));

    /// <summary>
    ///     Asynchronously projects the value of a <see cref="Task{Option}" /> using the specified function.
    /// </summary>
    public static async Task<Option<TResult>> SelectAwaitAsync<T, TResult>(this Task<Option<T>> task, Func<T, Task<TResult>> selector)
    {
        var option = await task;

        return option is Option<T>.Some some
                   ? new Option<TResult>.Some(await selector(some.Value))
                   : Option.None<TResult>();
    }
}
