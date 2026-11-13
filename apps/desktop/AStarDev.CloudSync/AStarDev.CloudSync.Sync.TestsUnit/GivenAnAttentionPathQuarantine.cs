using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAnAttentionPathQuarantine
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly DateTimeOffset Moment = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeRemoteIndexReader index = new();
    private readonly FakeSyncAttentionReader attention = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_nothing_is_queued_and_no_resync_runs_then_a_path_is_clear()
    {
        var state = await StateOf("Docs/A.txt");

        state.ShouldBe(PathQuarantineStateFactory.CreateClear());
    }

    [Theory]
    [InlineData("Docs/A.txt")]
    [InlineData("docs/a.TXT")]
    public async Task when_a_path_is_conflicted_then_it_is_conflicted_whatever_its_case(string path)
    {
        attention.Open = Items(Conflict("Docs/A.txt"));

        var state = await StateOf(path);

        state.ShouldBe(PathQuarantineStateFactory.CreateConflicted());
    }

    [Fact]
    public async Task when_a_folder_is_conflicted_then_everything_under_it_is_conflicted()
    {
        attention.Open = Items(Conflict("Docs"));

        var state = await StateOf("Docs/Deep/A.txt");

        state.ShouldBe(PathQuarantineStateFactory.CreateConflicted());
    }

    [Fact]
    public async Task when_a_conflict_is_open_then_a_sibling_is_clear()
    {
        attention.Open = Items(Conflict("Docs/A.txt"));

        var state = await StateOf("Docs/B.txt");

        state.ShouldBe(PathQuarantineStateFactory.CreateClear());
    }

    [Theory]
    [InlineData("Photos/One.jpg")]
    [InlineData("photos/TWO.jpg")]
    public async Task when_deletions_are_held_then_each_held_path_is_held_by_the_guard(string path)
    {
        attention.Open = Items(Held("Photos/One.jpg", "Photos/Two.jpg"));

        var state = await StateOf(path);

        state.ShouldBe(PathQuarantineStateFactory.CreateHeldByGuard());
    }

    [Fact]
    public async Task when_deletions_are_held_then_a_path_that_is_not_held_is_clear()
    {
        attention.Open = Items(Held("Photos/One.jpg"));

        var state = await StateOf("Photos/Three.jpg");

        state.ShouldBe(PathQuarantineStateFactory.CreateClear());
    }

    [Fact]
    public async Task when_a_path_is_both_conflicted_and_held_then_the_conflict_wins()
    {
        attention.Open = Items(Held("Docs/A.txt"), Conflict("docs/a.txt"));

        var state = await StateOf("Docs/A.txt");

        state.ShouldBe(PathQuarantineStateFactory.CreateConflicted());
    }

    [Fact]
    public async Task when_a_resync_runs_then_paths_without_an_entry_are_resync_in_progress_and_a_conflict_still_wins()
    {
        index.Pass = Result.Success<Option<ResyncPassId>, ErrorResponse>(Option.Some(new ResyncPassId(Guid.NewGuid())));
        attention.Open = Items(Conflict("Docs/A.txt"));

        var states = new[] { await StateOf("Docs/A.txt"), await StateOf("Docs/B.txt") };

        states.ShouldBe([PathQuarantineStateFactory.CreateConflicted(), PathQuarantineStateFactory.CreateResyncInProgress()]);
    }

    [Fact]
    public async Task when_a_skipped_path_is_queued_then_it_stays_skipped()
    {
        attention.Open = Items(Conflict(".obsidian/workspace.json"));

        var state = await StateOf(".obsidian/workspace.json");

        state.ShouldBe(PathQuarantineStateFactory.CreateSkipped());
    }

    [Fact]
    public async Task when_the_queue_cannot_be_read_then_the_read_fails_instead_of_reporting_clear()
    {
        attention.Open = Result.Failure<IReadOnlyList<SyncAttentionItem>, ErrorResponse>(new ErrorResponse("The queue is locked."));

        var read = await Quarantine().ReadAsync(Account, CancellationToken);

        read.Match(_ => "snapshot", error => error.Message).ShouldBe("The queue is locked.");
    }

    [Fact]
    public async Task when_the_resync_pass_cannot_be_read_then_the_read_fails_instead_of_reporting_clear()
    {
        index.Pass = Result.Failure<Option<ResyncPassId>, ErrorResponse>(new ErrorResponse("The index is locked."));

        var read = await Quarantine().ReadAsync(Account, CancellationToken);

        read.Match(_ => "snapshot", error => error.Message).ShouldBe("The index is locked.");
    }

    private static Result<IReadOnlyList<SyncAttentionItem>, ErrorResponse> Items(params SyncAttentionItem[] items) => Result.Success<IReadOnlyList<SyncAttentionItem>, ErrorResponse>(items);

    private static SyncAttentionItem Conflict(string path) => SyncAttentionItemFactory.CreateConflict(new ConflictId(Guid.NewGuid()), Account, SyncPaths.Of(path), ConflictKindFactory.CreateBothAdded(), SideSnapshotFactory.CreateAbsent(), SideSnapshotFactory.CreateAbsent(), Moment);

    private static SyncAttentionItem Held(params string[] paths) =>
        SyncAttentionItemFactory.CreateHeldDeletions(new HeldDeletionId(Guid.NewGuid()), Account, SyncPairIdFactory.Create(Guid.NewGuid()), DeletionDirectionFactory.CreateDeleteLocal(), paths.Select(SyncPaths.Of), Moment);

    private AttentionPathQuarantine Quarantine() => new(new ResyncPathQuarantine(index), attention);

    private async Task<PathQuarantineState> StateOf(string path)
    {
        var read = await Quarantine().ReadAsync(Account, CancellationToken);

        return read.Match(snapshot => snapshot.StateOf(SyncPaths.Of(path)), _ => throw new InvalidOperationException("The read must succeed."));
    }
}
