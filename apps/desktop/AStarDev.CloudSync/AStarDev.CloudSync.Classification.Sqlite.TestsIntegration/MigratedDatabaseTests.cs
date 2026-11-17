using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public abstract class MigratedDatabaseTests : IAsyncLifetime
{
    private static readonly DateTimeOffset BaselineWrittenAt = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    private TemporaryClassificationDatabase? database;

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    protected TemporaryClassificationDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public virtual async ValueTask InitializeAsync() => database = await TemporaryClassificationDatabase.CreateMigratedAsync(CancellationToken);

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (database is not null)
            await database.DisposeAsync();
    }

    protected static Task<T[]> InParallelAsync<T>(int count, Func<int, Task<T>> work) => Task.WhenAll(Enumerable.Range(0, count).Select(index => Task.Run(() => work(index), CancellationToken)));

    protected async Task InBaselineAsync(AccountId account, params string[] paths)
    {
        var entries = paths.Select((path, index) => SyncBaselineEntryFactory.Create(account, ClassificationData.Path(path), Valid(RemoteItemIdFactory.Create($"baseline-{index}")), SyncBaselineKindFactory.CreateFile(10, BaselineWrittenAt, Valid(ContentHashFactory.Create("h")), Valid(ContentTagFactory.Create("c")), Valid(EntityTagFactory.Create("e"))))).ToList();

        _ = Valid(await Database.BaselineWriter.UpsertAsync(account, entries, CancellationToken));
    }

    private static T Valid<T>(Result<T, ErrorResponse> result) => result.Match(value => value, _ => throw new InvalidOperationException("The test baseline value must be valid."));
}
