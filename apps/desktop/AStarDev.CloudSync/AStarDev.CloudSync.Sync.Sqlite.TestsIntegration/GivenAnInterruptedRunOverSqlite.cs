using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenAnInterruptedRunOverSqlite : IAsyncLifetime
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly ScriptedPasses passes = new();

    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync() =>
        database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken, services => services
            .AddCloudSync()
            .AddSingleton<IRemoteIndexUpdater>(passes)
            .AddSingleton<ILocalChangeDetector>(passes)
            .AddSingleton<IDownloadPass>(passes)
            .AddSingleton<IUploadPass>(passes));

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    [Fact]
    public async Task when_a_run_was_left_unfinished_then_recovery_marks_it_runs_a_recovery_pass_and_the_last_sync_is_read_from_sqlite()
    {
        _ = await Database.Journal.StartAsync(Account, SyncTriggerFactory.CreateScheduled(), CancellationToken);

        var outcomes = await Database.Services.GetRequiredService<IInterruptedSyncRecovery>().RecoverAsync(CancellationToken);

        outcomes.ShouldHaveSingleItem().ShouldBeOfType<RecoveryOutcome.Resumed>();
        (await Database.Journal.GetInterruptedAsync(CancellationToken)).Match(runs => runs.Count, _ => -1).ShouldBe(0);
        (await Database.Journal.GetLastSyncAsync(Account, CancellationToken)).Match(last => last.Match(_ => "synced", () => "never"), _ => "error").ShouldBe("synced");
    }

    [Fact]
    public async Task when_a_run_is_interrupted_then_the_activity_feed_in_sqlite_holds_the_interrupted_entry()
    {
        _ = await Database.Journal.StartAsync(Account, SyncTriggerFactory.CreateScheduled(), CancellationToken);

        _ = await Database.Services.GetRequiredService<IInterruptedSyncRecovery>().RecoverAsync(CancellationToken);

        (await Database.ActivityReader.GetRecentAsync(Option.Some(Account), 10, CancellationToken)).Match(entries => entries.Select(entry => entry.Kind), _ => []).ShouldContain(ActivityKindFactory.CreateSyncInterrupted());
    }
}
