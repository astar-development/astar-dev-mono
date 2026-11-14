using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenADatabaseHealthCheckOverSqlite : IAsyncLifetime
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

    private async Task<object> CheckAsync() => await Database.Health.CheckAsync(CancellationToken).MatchAsync(_ => (object)"sound", failure => failure);

    private async Task CorruptAsync()
    {
        await Database.RestartAsync();
        SqliteConnection.ClearAllPools();
        var bytes = await File.ReadAllBytesAsync(Database.DatabasePath, CancellationToken);
        Array.Fill(bytes, (byte)0xFF, 200, 1500);
        await File.WriteAllBytesAsync(Database.DatabasePath, bytes, CancellationToken);
    }

    [Fact]
    public async Task when_the_database_is_sound_then_the_check_passes()
    {
        (await CheckAsync()).ShouldBe("sound");
    }

    [Fact]
    public async Task when_the_database_file_does_not_exist_yet_then_the_check_passes_and_creates_nothing()
    {
        await using var fresh = TemporarySyncDatabase.CreateUnmigrated();

        var result = await fresh.Health.CheckAsync(CancellationToken).MatchAsync(_ => (object)"sound", failure => failure);

        (result, File.Exists(fresh.DatabasePath)).ShouldBe(("sound", false));
    }

    [Fact]
    public async Task when_the_file_is_not_a_database_then_the_check_fails()
    {
        await Database.RestartAsync();
        SqliteConnection.ClearAllPools();
        await File.WriteAllBytesAsync(Database.DatabasePath, "this is not a sqlite database, not even close"u8.ToArray(), CancellationToken);

        var result = await CheckAsync();

        result.ShouldBeAssignableTo<DatabaseHealthFailure>();
    }

    [Fact]
    public async Task when_the_database_file_is_corrupted_then_the_check_fails()
    {
        await CorruptAsync();

        var result = await CheckAsync();

        result.ShouldBeAssignableTo<DatabaseHealthFailure>();
    }

    [Fact]
    public async Task when_the_database_is_corrupted_then_a_sync_run_is_refused_and_recovery_leaves_the_interrupted_run_alone()
    {
        _ = await Database.Journal.StartAsync(Account, SyncTriggerFactory.CreateScheduled(), CancellationToken);
        await CorruptAsync();

        var run = await Database.Services.GetRequiredService<ISyncPassRunner>().RunAsync(Account, SyncTriggerFactory.CreateManual(), CancellationToken);
        var outcomes = await Database.Services.GetRequiredService<IInterruptedSyncRecovery>().RecoverAsync(CancellationToken);

        (run.Match(_ => (object)"ran", failure => failure), outcomes.Count).ShouldBe((SyncPassFailureFactory.CreateDatabaseUnavailable(), 0));
    }

    [Fact]
    public async Task when_the_database_is_corrupted_then_the_check_does_not_change_the_file()
    {
        await CorruptAsync();
        var before = await File.ReadAllBytesAsync(Database.DatabasePath, CancellationToken);

        _ = await CheckAsync();
        SqliteConnection.ClearAllPools();
        var after = await File.ReadAllBytesAsync(Database.DatabasePath, CancellationToken);

        after.ShouldBe(before);
    }

    [Fact]
    public async Task when_a_recovery_blocked_entry_is_stored_then_it_is_read_back_with_its_blocker()
    {
        var blockers = new[] { RecoveryBlockerFactory.CreateSignInRequired(), RecoveryBlockerFactory.CreateSyncFolderMissing(), RecoveryBlockerFactory.CreateDatabaseUnreadable() };
        var occurredAt = new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);
        var entries = blockers.Select((blocker, minute) => ActivityEntryFactory.Create(Account, Option.None<SyncPairId>(), ActivityKindFactory.CreateRecoveryBlocked(blocker), occurredAt.AddMinutes(minute))).ToArray();
        _ = await Database.ActivityWriter.AppendAsync(entries, CancellationToken);

        var read = (await Database.ActivityReader.GetRecentAsync(Option.Some(Account), 10, CancellationToken)).Match(found => found, _ => []);

        read.OrderBy(entry => entry.OccurredAt).Select(entry => entry.Kind).ShouldBe(entries.Select(entry => entry.Kind));
    }
}
