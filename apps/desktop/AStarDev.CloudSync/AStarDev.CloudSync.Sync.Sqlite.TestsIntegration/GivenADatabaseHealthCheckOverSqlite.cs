using AStarDev.CloudSync.TestHelpers.Common;
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

    private int ProbePageSize { get; set; }

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

    private async Task CorruptAsync(int from = 100)
    {
        await Database.RestartAsync();
        Database.ClearOwnPool();
        var bytes = await File.ReadAllBytesAsync(Database.DatabasePath, CancellationToken);
        Array.Fill(bytes, (byte)0xFF, from, bytes.Length - from);
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
        Database.ClearOwnPool();
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
    public async Task when_only_an_index_page_is_corrupted_then_the_integrity_check_reports_damage()
    {
        await Database.RestartAsync();
        var rootPage = await AddProbeIndexAsync();
        var bytes = await File.ReadAllBytesAsync(Database.DatabasePath, CancellationToken);
        Array.Fill(bytes, (byte)0xFF, ((rootPage - 1) * ProbePageSize) + 8, 32);
        await File.WriteAllBytesAsync(Database.DatabasePath, bytes, CancellationToken);

        var result = await CheckAsync();

        result.ShouldBe(DatabaseHealthFailureFactory.CreateDamaged());
    }

    private async Task<int> AddProbeIndexAsync()
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database.DatabasePath, Pooling = false }.ToString());
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE probe(id INTEGER PRIMARY KEY, v TEXT); CREATE INDEX probe_v ON probe(v); WITH RECURSIVE n(i) AS (SELECT 1 UNION ALL SELECT i + 1 FROM n WHERE i < 2000) INSERT INTO probe(v) SELECT 'value ' || i FROM n; PRAGMA journal_mode = DELETE;";
        _ = await command.ExecuteNonQueryAsync(CancellationToken);
        command.CommandText = "SELECT rootpage FROM sqlite_master WHERE name = 'probe_v'";
        var rootPage = Convert.ToInt32(await command.ExecuteScalarAsync(CancellationToken));
        command.CommandText = "PRAGMA page_size";
        ProbePageSize = Convert.ToInt32(await command.ExecuteScalarAsync(CancellationToken));

        return rootPage;
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
        Database.ClearOwnPool();
        var after = await File.ReadAllBytesAsync(Database.DatabasePath, CancellationToken);

        after.ShouldBe(before);
    }

    [Fact]
    public async Task when_a_recovery_blocked_entry_is_stored_then_it_is_read_back_with_its_blocker()
    {
        var blockers = RecoveryBlockerCases.All();
        var occurredAt = new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);
        var entries = blockers.Select((blocker, minute) => ActivityEntryFactory.Create(Account, Option.None<SyncPairId>(), ActivityKindFactory.CreateRecoveryBlocked(blocker), occurredAt.AddMinutes(minute))).ToArray();
        _ = await Database.ActivityWriter.AppendAsync(entries, CancellationToken);

        var read = (await Database.ActivityReader.GetRecentAsync(Option.Some(Account), 10, CancellationToken)).Match(found => found, _ => []);

        read.OrderBy(entry => entry.OccurredAt).Select(entry => entry.Kind).ShouldBe(entries.Select(entry => entry.Kind));
    }

    [Fact]
    public async Task when_a_stored_blocker_name_is_not_one_the_app_knows_then_the_entry_is_not_read_back_as_a_blocker()
    {
        _ = await Database.ActivityWriter.AppendAsync([ActivityEntryFactory.Create(Account, Option.None<SyncPairId>(), ActivityKindFactory.CreateAuthenticated(), new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero))], CancellationToken);
        await using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database.DatabasePath, Pooling = false }.ToString()))
        {
            await connection.OpenAsync(CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE ActivityLog SET Kind = 'RecoveryBlocked', FileName = 'ABlockerFromTheFuture'";
            _ = await command.ExecuteNonQueryAsync(CancellationToken);
        }

        var read = (await Database.ActivityReader.GetRecentAsync(Option.Some(Account), 10, CancellationToken)).Match(found => found, _ => []);

        read.ShouldHaveSingleItem().Kind.ShouldBeOfType<ActivityKind.DetectingChanges>();
    }
}
