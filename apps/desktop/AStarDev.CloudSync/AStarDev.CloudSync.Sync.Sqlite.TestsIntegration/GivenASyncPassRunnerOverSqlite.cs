using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenASyncPassRunnerOverSqlite : IAsyncLifetime
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly DateTimeOffset Moment = new(2026, 11, 14, 8, 0, 0, TimeSpan.Zero);

    private readonly ScriptedPasses passes = new();

    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    private static ConflictFinding Finding(string path, ConflictKind kind, SideSnapshot local, SideSnapshot remote) => ConflictFindingFactory.Create(SyncPaths.Of(path), kind, local, remote);

    private static object Outcome(Result<SyncPassSummary, SyncPassFailure> result) => result.Match(_ => (object)"ran", failure => failure);

    private async Task<IReadOnlyList<SyncAttentionItem>> OpenAsync() => (await Database.Services.GetRequiredService<ISyncAttentionReader>().GetOpenAsync(Account, CancellationToken)).Match(items => items, _ => throw new InvalidOperationException("The attention queue must be readable."));

    private Task<Result<SyncPassSummary, SyncPassFailure>> RunAsync() => Database.Services.GetRequiredService<ISyncPassRunner>().RunAsync(Account, SyncTriggerFactory.CreateManual(), CancellationToken);

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
    public async Task when_the_passes_find_conflicts_then_the_runner_writes_them_to_sqlite_and_they_read_back_with_both_snapshots()
    {
        var local = SideSnapshotFactory.CreatePresent(42, Moment, Option.Some(new ContentHash("hash-1")), Option.None<ContentTag>());
        var remote = SideSnapshotFactory.CreatePresent(43, Moment.AddMinutes(1), Option.Some(new ContentHash("hash-2")), Option.Some(new ContentTag("tag-1")));
        passes.Downloaded = [DownloadItemOutcomeFactory.CreateSentToConflicts(Finding("Docs/a.txt", ConflictKindFactory.CreateBothModified(), local, remote))];
        passes.Uploaded = [UploadItemOutcomeFactory.CreateSentToConflicts(Finding("Docs/b.txt", ConflictKindFactory.CreateLocalDeletedRemoteModified(), SideSnapshotFactory.CreateAbsent(), remote))];

        var result = await RunAsync();

        Outcome(result).ShouldBe("ran");
        var conflicts = (await OpenAsync()).OfType<SyncAttentionItem.Conflict>().OrderBy(conflict => conflict.Path.Value).ToList();
        conflicts.Select(conflict => (conflict.Path.Value, conflict.Kind, conflict.Local, conflict.Remote)).ShouldBe([("Docs/a.txt", ConflictKindFactory.CreateBothModified(), local, remote), ("Docs/b.txt", ConflictKindFactory.CreateLocalDeletedRemoteModified(), SideSnapshotFactory.CreateAbsent(), remote)]);
    }

    [Fact]
    public async Task when_the_download_and_upload_passes_report_the_same_path_in_different_case_then_one_row_is_stored()
    {
        var side = SideSnapshotFactory.CreatePresent(5, Moment, Option.None<ContentHash>(), Option.None<ContentTag>());
        passes.Downloaded = [DownloadItemOutcomeFactory.CreateSentToConflicts(Finding("Docs/A.txt", ConflictKindFactory.CreateBothAdded(), side, side))];
        passes.Uploaded = [UploadItemOutcomeFactory.CreateSentToConflicts(Finding("DOCS/a.TXT", ConflictKindFactory.CreateBothAdded(), side, side))];

        _ = await RunAsync();

        (await OpenAsync()).OfType<SyncAttentionItem.Conflict>().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task when_a_conflict_written_by_one_run_has_its_local_side_back_at_the_baseline_then_the_next_run_closes_it_in_sqlite()
    {
        var side = SideSnapshotFactory.CreatePresent(5, Moment, Option.None<ContentHash>(), Option.None<ContentTag>());
        passes.Downloaded = [DownloadItemOutcomeFactory.CreateSentToConflicts(Finding("Docs/a.txt", ConflictKindFactory.CreateBothModified(), side, side))];
        _ = await RunAsync();
        passes.Downloaded = [];
        _ = await Database.BaselineWriter.UpsertAsync(Account, [SyncBaselineEntryFactory.Create(Account, SyncPaths.Of("Docs/a.txt"), new RemoteItemId("item-a"), SyncBaselineKindFactory.CreateFile(5, Moment, new ContentHash("hash-1"), new ContentTag("tag-1"), new EntityTag("etag-1")))], CancellationToken);

        var result = await RunAsync();

        Outcome(result).ShouldBe("ran");
        (await OpenAsync()).ShouldBeEmpty();
    }
}
