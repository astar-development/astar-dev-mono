using AStarDev.FunctionalParadigm;
using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.DependencyInjection;
using static AStarDev.CloudSync.Sync.Sqlite.TestsIntegration.RemoteIndexData;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenAPathQuarantine : IAsyncLifetime
{
    private static readonly AccountId FirstAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId SecondAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    private IPathQuarantine Quarantine => Database.Services.GetRequiredService<IPathQuarantine>();

    public async ValueTask InitializeAsync() => database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken, services => services.AddCloudSync());

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    [Fact]
    public void when_the_sync_module_is_added_then_the_path_quarantine_is_one_singleton()
    {
        var first = Database.Services.GetRequiredService<IPathQuarantine>();
        var second = Database.Services.GetRequiredService<IPathQuarantine>();

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public async Task when_the_index_has_no_resync_then_a_path_is_clear()
    {
        _ = await Database.IndexWriter.ApplyPageAsync(FirstAccount, More("c1", File("f1", "root-id", "one.txt")), CancellationToken);

        var state = await StateOf(FirstAccount, "one.txt");

        state.ShouldBe(PathQuarantineStateFactory.CreateClear());
    }

    [Fact]
    public async Task when_a_resync_has_begun_then_every_path_of_the_account_is_resync_in_progress()
    {
        _ = await Database.IndexWriter.BeginResyncAsync(FirstAccount, CancellationToken);

        var states = new[] { await StateOf(FirstAccount, "one.txt"), await StateOf(FirstAccount, "Docs/Deep/two.txt"), await StateOf(FirstAccount, "DOCS/deep/TWO.txt") };

        states.ShouldAllBe(state => state is PathQuarantineState.ResyncInProgress);
    }

    [Fact]
    public async Task when_a_resync_has_begun_then_another_accounts_paths_are_clear()
    {
        _ = await Database.IndexWriter.BeginResyncAsync(FirstAccount, CancellationToken);

        var state = await StateOf(SecondAccount, "one.txt");

        state.ShouldBe(PathQuarantineStateFactory.CreateClear());
    }

    [Fact]
    public async Task when_the_resync_completes_then_the_paths_are_clear_again()
    {
        _ = await Database.IndexWriter.BeginResyncAsync(FirstAccount, CancellationToken);
        _ = await Database.IndexWriter.CompleteResyncAsync(FirstAccount, CancellationToken);

        var state = await StateOf(FirstAccount, "one.txt");

        state.ShouldBe(PathQuarantineStateFactory.CreateClear());
    }

    [Fact]
    public async Task when_a_resync_was_begun_before_a_restart_then_a_fresh_quarantine_still_reports_it()
    {
        _ = await Database.IndexWriter.BeginResyncAsync(FirstAccount, CancellationToken);
        var afterRestart = new ResyncPathQuarantine(Database.IndexReader);

        var read = await afterRestart.ReadAsync(FirstAccount, CancellationToken);

        read.Match(snapshot => snapshot.StateOf(SyncPaths("one.txt")), _ => PathQuarantineStateFactory.CreateClear()).ShouldBe(PathQuarantineStateFactory.CreateResyncInProgress());
    }

    [Fact]
    public async Task when_many_reads_race_a_resync_that_has_begun_then_each_reports_it()
    {
        _ = await Database.IndexWriter.BeginResyncAsync(FirstAccount, CancellationToken);

        var states = await Task.WhenAll(Enumerable.Range(0, 20).Select(index => Task.Run(() => StateOf(FirstAccount, index % 2 == 0 ? "Docs/a.txt" : "DOCS/A.TXT"), CancellationToken)));

        states.ShouldAllBe(state => state is PathQuarantineState.ResyncInProgress);
    }

    [Fact]
    public async Task when_the_token_is_already_cancelled_then_the_read_never_reports_clear()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var outcome = await Record.ExceptionAsync(async () => (await Quarantine.ReadAsync(FirstAccount, source.Token)).Match(_ => throw new InvalidOperationException("A cancelled read must not succeed."), error => error));

        (outcome is null || outcome is OperationCanceledException).ShouldBeTrue();
    }

    private static RelativeSyncPath SyncPaths(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));

    private async Task<PathQuarantineState> StateOf(AccountId account, string path)
    {
        var read = await Quarantine.ReadAsync(account, CancellationToken);

        return read.Match(snapshot => snapshot.StateOf(SyncPaths(path)), _ => throw new InvalidOperationException("The read must succeed."));
    }
}
