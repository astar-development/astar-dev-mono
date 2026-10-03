namespace AStarDev.ControlDb;

/// <summary>Defines the contract for a unit of work, which encapsulates a set of operations to be performed as a single transaction.</summary>
public interface IUnitOfWork
{
    /// <summary>Gets the repository for the specified aggregate type and key type.</summary>
    /// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
    /// <typeparam name="TKey">The type of the key for the aggregate root.
    /// Ideally, this should be a strongly-typed identifier.
    /// </typeparam>
    /// <returns>The repository for the specified aggregate type and key type.</returns>
    /// <example>
    /// var repository = unitOfWork.GetRepository&lt;MyAggregate, Guid&gt;();
    /// var repository = unitOfWork.GetRepository&lt;MyAggregate, StrongId&lt;Guid&gt;&gt;();
    /// </example>
    IRepository<TAggregate, TKey> GetRepository<TAggregate, TKey>() where TAggregate : IAggregateRoot;

    /// <summary>Runs <paramref name="operation"/> inside a database transaction: committed if it completes, rolled back if it throws, so several saves succeed or fail together.</summary>
    /// <typeparam name="T">The type of the operation's result.</typeparam>
    /// <param name="operation">The work to run; it may call <see cref="SaveChangesAsync"/> more than once.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>The operation's result.</returns>
    Task<T> InTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>Saves all changes made in this unit of work to the underlying database.</summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
