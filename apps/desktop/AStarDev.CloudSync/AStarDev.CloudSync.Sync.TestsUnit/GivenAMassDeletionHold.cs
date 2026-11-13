using AStarDev.FunctionalParadigm;
using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAMassDeletionHold : IDisposable
{
    private readonly LocalScanFixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => fixture.Dispose();

    [Fact]
    public async Task when_a_quarter_of_a_pair_is_missing_then_its_deletions_are_held_and_not_reported()
    {
        fixture.Include("Docs");
        fixture.SyncedFiles("Docs", 120);
        fixture.Lose("Docs", 60);

        var set = await fixture.ScanAsync(CancellationToken);

        (set.Changes.OfType<LocalChange.Deleted>().Count(), string.Join(';', set.Holds.Select(hold => $"{hold.PairId.Value}:{hold.Missing}:{hold.Total}"))).ShouldBe((0, $"{SelectionTestRules.PairAt("Docs").Value}:60:120"));
    }

    [Fact]
    public async Task when_the_deletions_are_held_then_the_held_set_is_recorded_for_the_pair_with_every_missing_path()
    {
        fixture.Include("Docs");
        fixture.SyncedFiles("Docs", 120);
        fixture.Lose("Docs", 60);

        _ = await fixture.ScanAsync(CancellationToken);

        var held = fixture.Attention.Held.ShouldHaveSingleItem();
        (held.AccountId, held.PairId, held.Direction, held.Paths.Count, held.Paths[0].Value).ShouldBe((LocalScanFixture.Account, SelectionTestRules.PairAt("Docs"), DeletionDirectionFactory.CreateDeleteRemote(), 60, "Docs/file000.txt"));
    }

    [Fact]
    public async Task when_the_deletions_are_held_then_the_adds_and_changes_of_the_pair_are_still_reported()
    {
        fixture.Include("Docs");
        fixture.SyncedFiles("Docs", 120);
        fixture.Lose("Docs", 60);
        _ = fixture.Write("Docs/new.txt", "hello");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Added:Docs/new.txt:File");
    }

    [Fact]
    public async Task when_fewer_than_fifty_items_are_missing_then_the_deletions_are_reported_and_nothing_is_held()
    {
        fixture.Include("Docs");
        fixture.SyncedFiles("Docs", 120);
        fixture.Lose("Docs", 49);

        var set = await fixture.ScanAsync(CancellationToken);

        (set.Changes.OfType<LocalChange.Deleted>().Count(), set.Holds.Count, fixture.Attention.Calls).ShouldBe((49, 0, 0));
    }

    [Fact]
    public async Task when_exactly_a_quarter_is_missing_then_the_deletions_are_reported()
    {
        fixture.Include("Docs");
        fixture.SyncedFiles("Docs", 200);
        fixture.Lose("Docs", 50);

        var set = await fixture.ScanAsync(CancellationToken);

        (set.Changes.OfType<LocalChange.Deleted>().Count(), set.Holds.Count).ShouldBe((50, 0));
    }

    [Fact]
    public async Task when_one_pair_trips_the_guard_then_the_other_pair_still_reports_its_deletions()
    {
        fixture.Include("Docs", "Pics");
        fixture.SyncedFiles("Docs", 120);
        fixture.SyncedFiles("Pics", 120);
        fixture.Lose("Docs", 60);
        fixture.Lose("Pics", 10);

        var set = await fixture.ScanAsync(CancellationToken);

        (string.Join(';', set.Holds.Select(hold => hold.PairId.Value)), set.Changes.OfType<LocalChange.Deleted>().Count(change => change.Path.Value!.StartsWith("Pics/", StringComparison.Ordinal))).ShouldBe(($"{SelectionTestRules.PairAt("Docs").Value}", 10));
    }

    [Fact]
    public async Task when_a_rule_nests_inside_a_pair_then_its_missing_items_count_towards_the_outer_pair()
    {
        fixture.Include("Docs", "Docs/Work");
        fixture.SyncedFiles("Docs", 60);
        fixture.SyncedFiles("Docs/Work", 60);
        fixture.Lose("Docs/Work", 60);

        var set = await fixture.ScanAsync(CancellationToken);

        string.Join(';', set.Holds.Select(hold => $"{hold.PairId.Value}:{hold.Missing}:{hold.Total}")).ShouldBe($"{SelectionTestRules.PairAt("Docs").Value}:60:120");
    }

    [Fact]
    public async Task when_nothing_is_held_then_no_attention_item_is_written()
    {
        fixture.Include("Docs");
        fixture.SyncedFiles("Docs", 120);
        fixture.Lose("Docs", 3);

        _ = await fixture.ScanAsync(CancellationToken);

        fixture.Attention.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_hold_cannot_be_recorded_then_the_scan_fails_and_no_deletion_is_reported()
    {
        fixture.Include("Docs");
        fixture.SyncedFiles("Docs", 120);
        fixture.Lose("Docs", 60);
        fixture.Attention.Fails = true;

        var result = await fixture.Detector.DetectAsync(LocalScanFixture.Account, CancellationToken);

        result.Match(_ => "scanned", failure => failure.GetType().Name).ShouldBe(nameof(LocalScanFailure.Unexpected));
    }

    [Fact]
    public async Task when_the_deletions_are_held_then_the_log_carries_counts_and_no_path()
    {
        fixture.Include("Docs");
        fixture.SyncedFiles("Docs", 120);
        fixture.Lose("Docs", 60);

        _ = await fixture.ScanAsync(CancellationToken);

        fixture.Logs.Logger.Messages.ShouldNotContain(message => message.Contains("file0", StringComparison.Ordinal) || message.Contains("Docs", StringComparison.Ordinal));
    }
}
