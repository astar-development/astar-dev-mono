namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     LINQ extensions that return <see cref="Option{T}" /> values, bridging the functional and imperative paradigms.
/// </summary>
public static class LinqExtensions
{
    /// <summary>
    ///     Returns the first element of <paramref name="source" /> as an <see cref="Option{T}" />, or <c>None</c> if the
    ///     sequence is empty.
    /// </summary>
    /// <param name="source">The sequence to search.</param>
    /// <typeparam name="T">The element type.</typeparam>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source) =>
        source.Select(x => new Option<T>.Some(x))
              .DefaultIfEmpty(Option.None<T>())
              .First();

    /// <summary>
    ///     Returns the first element of <paramref name="source" /> matching <paramref name="predicate" /> as an
    ///     <see cref="Option{T}" />, or <c>None</c> if no element matches.
    /// </summary>
    /// <param name="source">The sequence to search.</param>
    /// <param name="predicate">The condition an element must satisfy.</param>
    /// <typeparam name="T">The element type.</typeparam>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate) =>
        source.Where(predicate)
              .Select<T, Option<T>>(x => x)
              .DefaultIfEmpty(Option.None<T>())
              .First();

    /// <summary>
    ///     Asynchronously returns the first element of <paramref name="source" /> as an <see cref="Option{T}" />, or
    ///     <c>None</c> if the sequence is empty.
    /// </summary>
    /// <param name="source">The sequence to search.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the enumeration.</param>
    /// <typeparam name="T">The element type.</typeparam>
    public static async Task<Option<T>> FirstOrNoneAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default) =>
        await source
              .Select<T, Option<T>>(x => x)
              .DefaultIfEmpty(Option.None<T>())
              .FirstAsync(cancellationToken);

    /// <summary>
    ///     Asynchronously returns the first element of <paramref name="source" /> matching <paramref name="predicate" />
    ///     as an <see cref="Option{T}" />, or <c>None</c> if no element matches.
    /// </summary>
    /// <param name="source">The sequence to search.</param>
    /// <param name="predicate">The condition an element must satisfy.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the enumeration.</param>
    /// <typeparam name="T">The element type.</typeparam>
    public static async Task<Option<T>> FirstOrNoneAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default) =>
        await source.Where(predicate)
              .Select<T, Option<T>>(x => x)
              .DefaultIfEmpty(Option.None<T>())
              .FirstAsync(cancellationToken);
}
