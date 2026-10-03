using AStarDev.ControlDb;
using AStarDev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.TestsUnit.Fakes;

/// <summary>An in-memory repository whose lookup results are set by the test and which records what was added and deleted.</summary>
internal sealed class FakeRepository<TAggregate, TKey>(List<string> operations) : IRepository<TAggregate, TKey>
    where TAggregate : IAggregateRoot
{
    public FakeRepository()
        : this([])
    {
    }

    /// <summary>The result of <see cref="TryGetFirstAsync"/>.</summary>
    public Exceptional<Option<TAggregate>> First { get; set; } = Option<TAggregate>.None.Instance;

    /// <summary>The result of <see cref="TryFindAsync"/>.</summary>
    public Exceptional<Option<TAggregate>> Found { get; set; } = Option<TAggregate>.None.Instance;

    /// <summary>The failure returned by <see cref="Add"/>, or none for it to succeed.</summary>
    public Option<Exception> AddFailure { get; set; } = Option.None<Exception>();

    /// <summary>The failure returned by <see cref="Delete"/>, or none for it to succeed.</summary>
    public Option<Exception> DeleteFailure { get; set; } = Option.None<Exception>();

    /// <summary>Simulates the store completing an added aggregate (for example assigning its id). It is applied before the aggregate is recorded and returned.</summary>
    public Func<TAggregate, TAggregate> Store { get; set; } = aggregate => aggregate;

    /// <summary>The aggregates passed to <see cref="Add"/> that did not fail, as completed by <see cref="Store"/>.</summary>
    public List<TAggregate> Added { get; } = [];

    /// <summary>The aggregates passed to <see cref="Delete"/> that did not fail.</summary>
    public List<TAggregate> Deleted { get; } = [];

    public Task<Exceptional<Option<TAggregate>>> TryFindAsync(TKey key, CancellationToken cancellationToken = default) => cancellationToken.IsCancellationRequested ? Task.FromCanceled<Exceptional<Option<TAggregate>>>(cancellationToken) : Task.FromResult(Found);

    public Task<Exceptional<Option<TAggregate>>> TryGetFirstAsync(CancellationToken cancellationToken = default) => cancellationToken.IsCancellationRequested ? Task.FromCanceled<Exceptional<Option<TAggregate>>>(cancellationToken) : Task.FromResult(First);

    public Exceptional<TAggregate> Add(TAggregate aggregate)
    {
        if (AddFailure is Option<Exception>.Some failure) return failure.Value;

        var stored = Store(aggregate);
        Added.Add(stored);
        operations.Add("add");

        return stored;
    }

    public Exceptional<Unit> Delete(TAggregate aggregate)
    {
        if (DeleteFailure is Option<Exception>.Some failure) return failure.Value;

        Deleted.Add(aggregate);
        operations.Add("delete");

        return Unit.Instance;
    }
}
