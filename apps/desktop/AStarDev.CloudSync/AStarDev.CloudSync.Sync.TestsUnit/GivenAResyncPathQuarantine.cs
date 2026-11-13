using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAResyncPathQuarantine
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly FakeRemoteIndexReader reader = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_no_resync_is_running_then_a_path_is_clear()
    {
        var quarantine = new ResyncPathQuarantine(reader);

        var state = await StateOf(quarantine, "Documents/Report.docx");

        state.ShouldBe(PathQuarantineStateFactory.CreateClear());
    }

    [Theory]
    [InlineData("Documents/Report.docx")]
    [InlineData("documents/REPORT.DOCX")]
    [InlineData("Photos")]
    public async Task when_a_resync_is_running_then_every_path_is_resync_in_progress(string path)
    {
        reader.Pass = Running();
        var quarantine = new ResyncPathQuarantine(reader);

        var state = await StateOf(quarantine, path);

        state.ShouldBe(PathQuarantineStateFactory.CreateResyncInProgress());
    }

    [Fact]
    public async Task when_a_resync_is_running_then_a_skipped_path_stays_skipped()
    {
        reader.Pass = Running();
        var quarantine = new ResyncPathQuarantine(reader);

        var state = await StateOf(quarantine, ".obsidian/workspace.json");

        state.ShouldBe(PathQuarantineStateFactory.CreateSkipped());
    }

    [Fact]
    public async Task when_the_index_cannot_be_read_then_the_read_fails_instead_of_reporting_clear()
    {
        reader.Pass = Result.Failure<Option<ResyncPassId>, ErrorResponse>(new ErrorResponse("The index is locked."));
        var quarantine = new ResyncPathQuarantine(reader);

        var read = await quarantine.ReadAsync(Account, CancellationToken);

        read.Match(_ => "snapshot", error => error.Message).ShouldBe("The index is locked.");
    }

    [Fact]
    public async Task when_the_snapshot_is_read_then_the_account_and_token_reach_the_index()
    {
        using var source = new CancellationTokenSource();
        var quarantine = new ResyncPathQuarantine(reader);

        _ = await quarantine.ReadAsync(Account, source.Token);

        (reader.SeenAccount, reader.SeenToken).ShouldBe((Account, source.Token));
    }

    private static Result<Option<ResyncPassId>, ErrorResponse> Running() => Result.Success<Option<ResyncPassId>, ErrorResponse>(Option.Some(new ResyncPassId(Guid.NewGuid())));

    private static async Task<PathQuarantineState> StateOf(IPathQuarantine quarantine, string path)
    {
        var read = await quarantine.ReadAsync(Account, CancellationToken);

        return read.Match(snapshot => snapshot.StateOf(SyncPaths.Of(path)), _ => throw new InvalidOperationException("The read must succeed."));
    }
}
