using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using static AStarDev.CloudSync.Sync.Sqlite.TestsIntegration.RemoteIndexData;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenARecoveryOfAnInterruptedEnumerationOverSqlite : IAsyncLifetime
{
    private readonly ScriptedChangeFeed feed = new();
    private readonly ScriptedPasses passes = new();

    private TemporarySyncDatabase? database;
    private AccountId account;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync()
    {
        database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken, services => services
            .AddCloudSync()
            .AddKeyedSingleton<IRemoteChangeFeed>(CloudProviderFactory.CreateOneDrive(), feed)
            .AddSingleton<ILocalChangeDetector>(passes)
            .AddSingleton<IDownloadPass>(passes)
            .AddSingleton<IUploadPass>(passes));
        var key = ProviderAccountKeyFactory.Create("key-1").Match(created => created, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(key, CloudProviderFactory.CreateOneDrive(), "Personal", "personal@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1, 2));
        account = (await Database.AccountStore.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    private async Task InterruptAfterTheFirstPageAsync()
    {
        feed.Enqueue(More("c1", File("f1", "root-id", "one.txt")));
        feed.Enqueue(ProviderErrorFactory.CreateTransient());
        _ = await Database.Services.GetRequiredService<IRemoteIndexUpdater>().UpdateAsync(account, CancellationToken);
        _ = await Database.Journal.StartAsync(account, SyncTriggerFactory.CreateScheduled(), CancellationToken);
    }

    [Fact]
    public async Task when_a_pass_was_interrupted_after_the_first_delta_page_then_recovery_asks_the_feed_to_continue_from_the_saved_cursor()
    {
        await InterruptAfterTheFirstPageAsync();
        feed.Enqueue(Caught("c3", File("f3", "root-id", "three.txt")));

        var outcomes = await Database.Services.GetRequiredService<IInterruptedSyncRecovery>().RecoverAsync(CancellationToken);

        outcomes.ShouldHaveSingleItem().ShouldBeOfType<RecoveryOutcome.Resumed>();
        feed.Requested.ShouldBe(["none", "c1", "c1"]);
    }

    [Fact]
    public async Task when_recovery_finishes_the_enumeration_then_the_final_cursor_is_saved_and_the_run_is_no_longer_interrupted()
    {
        await InterruptAfterTheFirstPageAsync();
        feed.Enqueue(Caught("c3"));

        _ = await Database.Services.GetRequiredService<IInterruptedSyncRecovery>().RecoverAsync(CancellationToken);

        (await Database.IndexReader.GetCursorAsync(account, CancellationToken)).Match(found => found.Match(cursor => cursor.Value, () => "none"), _ => "error").ShouldBe("c3");
        (await Database.Journal.GetInterruptedAsync(CancellationToken)).Match(runs => runs.Count, _ => -1).ShouldBe(0);
    }
}
