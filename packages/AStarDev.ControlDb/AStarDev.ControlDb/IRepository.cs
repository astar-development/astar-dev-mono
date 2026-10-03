using AStarDev.FunctionalParadigm;

namespace AStarDev.ControlDb;

/// <summary>Represents a repository for managing aggregates of type <typeparamref name="TAggregate"/> with keys of type <typeparamref name="TKey"/>.</summary>
public interface IRepository<TAggregate, TKey> where TAggregate : IAggregateRoot
{
    /// <summary>Tries to find an aggregate by its key. Returns an exceptional result containing an option of the aggregate if found, or an empty option if not found.</summary>
    /// <param name="key">The key of the aggregate to find.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>An exceptional result containing an option of the aggregate if found, or an empty option if not found.</returns>
    Task<Exceptional<Option<TAggregate>>> TryFindAsync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>Tries to get the first aggregate from the repository. Returns an exceptional result containing an option of the aggregate if found, or an empty option if not found.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>An exceptional result containing an option of the aggregate if found, or an empty option if not found.</returns>
    Task<Exceptional<Option<TAggregate>>> TryGetFirstAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new aggregate to the repository. Returns an exceptional result containing the added aggregate if successful.</summary>
    /// <param name="aggregate">The aggregate to add.</param>
    /// <returns>An exceptional result containing the added aggregate if successful.</returns>
    Exceptional<TAggregate> Add(TAggregate aggregate);

    /// <summary>Deletes an aggregate from the repository. Returns an exceptional result indicating the success or failure of the operation.</summary>
    /// <param name="aggregate">The aggregate to delete.</param>
    /// <returns>An exceptional result indicating the success or failure of the operation.</returns>
    Exceptional<Unit> Delete(TAggregate aggregate);

    /// <summary>Deletes an aggregate by its key from the repository. Returns an exceptional result indicating the success or failure of the operation.</summary>
    /// <param name="key">The key of the aggregate to delete.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>An exceptional result indicating the success or failure of the operation.</returns>
    public async Task<Exceptional<Unit>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
        => (await TryFindAsync(key, cancellationToken).ConfigureAwait(false)).Match(
            findById => findById.Match(
                aggregate => Delete(aggregate),
                () => Unit.Instance
            ),
            ex => ex
        );
}
