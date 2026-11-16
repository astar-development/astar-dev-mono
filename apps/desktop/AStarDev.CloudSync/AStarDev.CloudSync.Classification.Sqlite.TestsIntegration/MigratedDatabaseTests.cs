namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public abstract class MigratedDatabaseTests : IAsyncLifetime
{
    private TemporaryClassificationDatabase? database;

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    protected TemporaryClassificationDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync() => database = await TemporaryClassificationDatabase.CreateMigratedAsync(CancellationToken);

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (database is not null)
            await database.DisposeAsync();
    }

    protected static Task<T[]> InParallelAsync<T>(int count, Func<int, Task<T>> work) => Task.WhenAll(Enumerable.Range(0, count).Select(index => Task.Run(() => work(index), CancellationToken)));
}
