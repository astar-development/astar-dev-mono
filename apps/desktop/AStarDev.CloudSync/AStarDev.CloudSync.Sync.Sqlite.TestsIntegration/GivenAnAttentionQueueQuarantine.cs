using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenAnAttentionQueueQuarantine : IAsyncLifetime
{
    private static readonly AccountId FirstAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId SecondAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateTimeOffset Moment = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);

    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync() => database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken, services => services.AddCloudSync());

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    [Fact]
    public async Task when_a_conflict_is_stored_then_the_quarantine_reports_it_for_that_account_only()
    {
        await RecordAsync(Conflict(FirstAccount, "Docs/A.txt"));

        var states = new[] { await StateOf(FirstAccount, "docs/a.TXT"), await StateOf(FirstAccount, "Docs/B.txt"), await StateOf(SecondAccount, "Docs/A.txt") };

        states.ShouldBe([PathQuarantineStateFactory.CreateConflicted(), PathQuarantineStateFactory.CreateClear(), PathQuarantineStateFactory.CreateClear()]);
    }

    [Fact]
    public async Task when_deletions_are_held_then_the_quarantine_reports_the_held_paths()
    {
        await RecordAsync(SyncAttentionItemFactory.CreateHeldDeletions(new HeldDeletionId(Guid.NewGuid()), FirstAccount, SyncPairIdFactory.Create(Guid.NewGuid()), DeletionDirectionFactory.CreateDeleteLocal(), [SyncPaths.Of("Photos/One.jpg")], Moment));

        var states = new[] { await StateOf(FirstAccount, "photos/one.JPG"), await StateOf(FirstAccount, "Photos/Two.jpg") };

        states.ShouldBe([PathQuarantineStateFactory.CreateHeldByGuard(), PathQuarantineStateFactory.CreateClear()]);
    }

    [Fact]
    public async Task when_a_conflict_is_closed_then_the_path_is_clear_again()
    {
        var conflict = (SyncAttentionItem.Conflict)Conflict(FirstAccount, "Docs/A.txt");
        await RecordAsync(conflict);

        _ = await Database.Services.GetRequiredService<ISyncAttentionCloser>().CloseAsync(conflict.Id, AttentionResolutionFactory.CreateKeepCloud(), CancellationToken);

        (await StateOf(FirstAccount, "Docs/A.txt")).ShouldBe(PathQuarantineStateFactory.CreateClear());
    }

    [Fact]
    public async Task when_a_resync_runs_and_a_conflict_is_stored_then_the_conflict_wins_for_its_path_and_the_rest_is_resyncing()
    {
        await RecordAsync(Conflict(FirstAccount, "Docs/A.txt"));
        _ = await Database.IndexWriter.BeginResyncAsync(FirstAccount, CancellationToken);

        var states = new[] { await StateOf(FirstAccount, "Docs/A.txt"), await StateOf(FirstAccount, "Docs/B.txt") };

        states.ShouldBe([PathQuarantineStateFactory.CreateConflicted(), PathQuarantineStateFactory.CreateResyncInProgress()]);
    }

    [Fact]
    public async Task when_many_reads_race_a_write_then_each_read_succeeds()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(index => Task.Run(async () =>
        {
            if (index % 4 == 0)
                await RecordAsync(Conflict(FirstAccount, $"Docs/File{index}.txt"));

            return await Database.Services.GetRequiredService<IPathQuarantine>().ReadAsync(FirstAccount, CancellationToken);
        }, CancellationToken)));

        results.ShouldAllBe(result => result.Match(_ => true, _ => false));
    }

    private static SyncAttentionItem Conflict(AccountId account, string path) =>
        SyncAttentionItemFactory.CreateConflict(new ConflictId(Guid.NewGuid()), account, SyncPaths.Of(path), ConflictKindFactory.CreateBothAdded(), SideSnapshotFactory.CreateAbsent(), SideSnapshotFactory.CreateAbsent(), Moment);

    private async Task RecordAsync(SyncAttentionItem item) => _ = await Database.Services.GetRequiredService<ISyncAttentionWriter>().RecordAsync([item], CancellationToken);

    private async Task<PathQuarantineState> StateOf(AccountId account, string path)
    {
        var read = await Database.Services.GetRequiredService<IPathQuarantine>().ReadAsync(account, CancellationToken);

        return read.Match(snapshot => snapshot.StateOf(SyncPaths.Of(path)), _ => throw new InvalidOperationException("The read must succeed."));
    }
}
