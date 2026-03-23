namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Contains extensions for <see cref="IEnumerable{T}" />.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    ///     An extension method that, rather than returning null if no object matches the predicate, it will return a suitable instance of <see cref="Option{T}.None" />.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    /// <param name="sequence">The sequence of objects to search.</param>
    /// <param name="predicate">The predicate to apply.</param>
    /// <returns>The option, containing the item or a suitable instance of <see cref="Option{T}.None" />.</returns>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> sequence, Func<T, bool> predicate) =>
        sequence.Where(predicate)
                .Select<T, Option<T>>(x => x)
                .DefaultIfEmpty(Option.None<T>())
                .First();
}
