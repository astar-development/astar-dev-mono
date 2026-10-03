using AStarDev.ControlDb;

namespace AStarDev.WallpaperScraper.TestsUnit.Fakes;

/// <summary>A unit of work that hands out registered repositories and counts how many times it was saved.</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly Dictionary<Type, object> repositories = [];

    /// <summary>The ordered log of repository operations and saves (<c>add</c>, <c>delete</c>, <c>save</c>) shared with the repositories created by <see cref="Register{TAggregate, TKey}"/>.</summary>
    public List<string> Operations { get; } = [];

    /// <summary>The number of transactions started through <see cref="InTransactionAsync{T}"/>.</summary>
    public int TransactionCount { get; private set; }

    /// <summary>The number of times <see cref="SaveChangesAsync"/> was called.</summary>
    public int SaveCount { get; private set; }

    /// <summary>The cancellation token passed to each <see cref="SaveChangesAsync"/> call, in order.</summary>
    public List<CancellationToken> SaveTokens { get; } = [];

    /// <summary>Runs on each <see cref="SaveChangesAsync"/> call before it is counted, so a test can make a save throw.</summary>
    public Action<CancellationToken> OnSave { get; set; } = _ => { };

    /// <summary>Creates a repository whose operations are logged in <see cref="Operations"/> and makes it available to <see cref="GetRepository{TAggregate, TKey}"/>.</summary>
    public FakeRepository<TAggregate, TKey> Register<TAggregate, TKey>()
        where TAggregate : IAggregateRoot
    {
        var repository = new FakeRepository<TAggregate, TKey>(Operations);
        repositories[typeof(TAggregate)] = repository;

        return repository;
    }

    public IRepository<TAggregate, TKey> GetRepository<TAggregate, TKey>()
        where TAggregate : IAggregateRoot =>
        (IRepository<TAggregate, TKey>)repositories[typeof(TAggregate)];

    public Task<T> InTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        TransactionCount++;

        return operation();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return Task.FromCanceled<int>(cancellationToken);

        OnSave(cancellationToken);
        SaveCount++;
        SaveTokens.Add(cancellationToken);
        Operations.Add("save");

        return Task.FromResult(1);
    }
}
