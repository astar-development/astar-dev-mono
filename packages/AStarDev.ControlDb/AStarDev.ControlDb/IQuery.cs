namespace AStarDev.ControlDb;

/// <summary>Defines the eager-loading (include) rules to apply when querying an aggregate of type <typeparamref name="TAggregate"/>.</summary>
public interface IQuery<TAggregate> where TAggregate : IAggregateRoot
{
    /// <summary>Applies the required includes to the supplied queryable.</summary>
    /// <param name="query">The queryable to apply the includes to.</param>
    /// <returns>The queryable with the required includes applied.</returns>
    IQueryable<TAggregate> Apply(IQueryable<TAggregate> query);
}

