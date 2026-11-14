using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenARunFinishingAfterAccountRemoval : IAsyncLifetime
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly ScriptedPasses passes = new();

    private GatedActivityLogWriter? writer;
    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    private GatedActivityLogWriter Writer => writer ?? throw new InvalidOperationException("The writer is created with the database.");

    public async ValueTask InitializeAsync() =>
        database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken, services =>
        {
            var original = services.Last(descriptor => descriptor.ServiceType == typeof(IActivityLogWriter));
            _ = services
                .AddCloudSync()
                .AddSingleton<IRemoteIndexUpdater>(passes)
                .AddSingleton<ILocalChangeDetector>(passes)
                .AddSingleton<IDownloadPass>(passes)
                .AddSingleton<IUploadPass>(passes)
                .AddSingleton<IActivityLogWriter>(provider => writer = new GatedActivityLogWriter((IActivityLogWriter)original.ImplementationFactory!(provider)));
        });

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    [Fact]
    public async Task when_a_run_writes_its_activity_after_the_account_was_removed_then_no_activity_rows_remain()
    {
        passes.Downloaded = [DownloadItemOutcomeFactory.CreateDownloaded(100)];
        _ = Database.Services.GetRequiredService<IActivityLogWriter>();
        var run = Task.Run(() => Database.Services.GetRequiredService<ISyncPassRunner>().RunAsync(Account, SyncTriggerFactory.CreateManual(), CancellationToken), CancellationToken);
        await Writer.Reached.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);
        var lease = Database.Services.GetRequiredService<IAccountSyncLock>().TryAcquire(Account);
        _ = await Database.ActivityParticipant.RemoveAccountDataAsync(Account, CancellationToken);

        Writer.Release();
        _ = await run;

        (lease is SyncLockAttempt.Acquired).ShouldBeTrue();
        (await Database.ActivityReader.GetRecentAsync(Option.Some(Account), 10, CancellationToken)).Match(found => found, _ => []).ShouldBeEmpty();
    }
}
