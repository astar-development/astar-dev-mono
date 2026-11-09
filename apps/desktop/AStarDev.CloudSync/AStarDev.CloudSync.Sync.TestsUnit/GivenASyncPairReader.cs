using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenASyncPairReader
{
    private readonly ScriptedRuleReader rules = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_there_are_no_rules_then_there_are_no_pairs()
    {
        var pairs = await ReadAsync();

        pairs.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_folder_is_included_then_it_is_a_pair_with_its_id_name_path_remote_folder_and_direction()
    {
        Rules(Include("Docs/Work", SyncDirectionFactory.CreateUploadOnly(), "remote-1"));

        var pair = (await ReadAsync()).ShouldHaveSingleItem();

        (pair.Id, pair.AccountId, pair.Name, pair.LocalFolder.Value, pair.RemoteFolder.Match(id => id.Value, () => "none"), pair.Direction).ShouldBe((PairAt("Docs/Work"), Account, "Work", "Docs/Work", "remote-1", SyncDirectionFactory.CreateUploadOnly()));
    }

    [Fact]
    public async Task when_a_pair_has_no_stored_status_then_it_reads_as_up_to_date_with_no_files_and_never_synced()
    {
        Rules(Include("Docs"));

        var pair = (await ReadAsync()).ShouldHaveSingleItem();

        (pair.Status, pair.FileCount, pair.LastSync.Match(_ => "synced", () => "never")).ShouldBe((SyncStatusFactory.CreateUpToDate(), 0, "never"));
    }

    [Fact]
    public async Task when_an_include_is_inside_another_include_then_only_the_top_most_one_is_a_pair()
    {
        Rules(Include("Docs"), Include("Docs/Sub"), Exclude("Docs/Private"), Include("Docs/Private/Keep"));

        var pairs = await ReadAsync();

        pairs.Select(pair => pair.LocalFolder.Value).ShouldBe(["Docs"]);
    }

    [Fact]
    public async Task when_an_include_is_inside_an_excluded_folder_that_is_not_in_a_pair_then_it_is_its_own_pair()
    {
        Rules(Exclude("Docs"), Include("Docs/Keep"));

        var pairs = await ReadAsync();

        pairs.Select(pair => pair.LocalFolder.Value).ShouldBe(["Docs/Keep"]);
    }

    [Fact]
    public async Task when_there_are_only_excludes_then_there_are_no_pairs()
    {
        Rules(Exclude("Docs"), Exclude("Photos"));

        var pairs = await ReadAsync();

        pairs.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_there_are_several_pairs_then_they_are_sorted_by_path_ignoring_case()
    {
        Rules(Include("zeta"), Include("Alpha"), Include("beta"));

        var pairs = await ReadAsync();

        pairs.Select(pair => pair.LocalFolder.Value).ShouldBe(["Alpha", "beta", "zeta"]);
    }

    [Fact]
    public async Task when_a_nested_include_differs_from_its_parent_only_in_case_then_it_is_not_a_second_pair()
    {
        Rules(Include("Docs"), Include("docs/Sub"));

        var pairs = await ReadAsync();

        pairs.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_root_is_included_then_it_is_the_only_pair()
    {
        Rules(Include(string.Empty), Include("Docs"));

        var pair = (await ReadAsync()).ShouldHaveSingleItem();

        (pair.LocalFolder, pair.Name).ShouldBe((LocalRelativePathFactory.Root, string.Empty));
    }

    [Fact]
    public async Task when_the_rules_cannot_be_read_then_the_read_fails_rather_than_returning_no_pairs()
    {
        rules.Listing = Result.Failure<IReadOnlyList<SelectionRule>, ErrorResponse>(new ErrorResponse("rules failed"));

        var result = await new SyncPairReader(rules).GetAsync(Account, CancellationToken);

        result.Match(_ => "read", error => error.Message).ShouldBe("rules failed");
    }

    private void Rules(params SelectionRule[] stored) => rules.Listing = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>(stored);

    private async Task<List<SyncPair>> ReadAsync() => [.. (await new SyncPairReader(rules).GetAsync(Account, CancellationToken)).Match(pairs => pairs, _ => throw new InvalidOperationException("The pairs must be readable."))];
}
