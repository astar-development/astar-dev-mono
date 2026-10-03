using AStarDev.FunctionalParadigm;

namespace AStarDev.FunctionalParadigm;

/// <summary>LINQ extensions that return <see cref="Option{T}" /> values, bridging the functional and imperative paradigms.</summary>
public static class LinqExtensions
{
    extension<T>(IEnumerable<T> source)
    {
        /// <summary>Returns the first element of <paramref name="source" /> as an <see cref="Option{T}" />, or <c>None</c> if the sequence is empty.</summary>
        public Option<T> FirstOrNone() =>
            source.Select(x => new Option<T>.Some(x))
                  .DefaultIfEmpty(Option.None<T>())
                  .First();

        /// <summary>Returns the first element of <paramref name="source" /> matching <paramref name="predicate" /> as an <see cref="Option{T}" />, or <c>None</c> if no element matches.</summary>
        /// <param name="predicate">The condition an element must satisfy.</param>
        public Option<T> FirstOrNone(Func<T, bool> predicate) =>
            source.Where(predicate)
                  .Select<T, Option<T>>(x => x)
                  .DefaultIfEmpty(Option.None<T>())
                  .First();
    }

    extension<T>(IAsyncEnumerable<T> source)
    {
        /// <summary>Asynchronously returns the first element of <paramref name="source" /> as an <see cref="Option{T}" />, or <c>None</c> if the sequence is empty.</summary>
        /// <param name="cancellationToken">A token used to observe cancellation of the enumeration.</param>
        public async Task<Option<T>> FirstOrNoneAsync(CancellationToken cancellationToken = default) =>
            await source
                  .Select<T, Option<T>>(x => x)
                  .DefaultIfEmpty(Option.None<T>())
                  .FirstAsync(cancellationToken);

        /// <summary>
        ///     Asynchronously returns the first element of <paramref name="source" /> matching <paramref name="predicate" />
        ///     as an <see cref="Option{T}" />, or <c>None</c> if no element matches.
        /// </summary>
        /// <param name="predicate">The condition an element must satisfy.</param>
        /// <param name="cancellationToken">A token used to observe cancellation of the enumeration.</param>
        public async Task<Option<T>> FirstOrNoneAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default) =>
            await source.Where(predicate)
                  .Select<T, Option<T>>(x => x)
                  .DefaultIfEmpty(Option.None<T>())
                  .FirstAsync(cancellationToken);
    }
}
