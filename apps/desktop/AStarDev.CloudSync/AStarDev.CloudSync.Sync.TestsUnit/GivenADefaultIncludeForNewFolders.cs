using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenADefaultIncludeForNewFolders : IDisposable
{
    private readonly LocalScanFixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => fixture.Dispose();

    private string[] Included() => [.. fixture.RuleWriter.Includes.Select(include => include.Path.Value!)];

    [Fact]
    public async Task when_a_local_folder_has_no_rule_and_is_not_remote_then_it_gets_a_two_way_include_rule()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Photos/a.jpg", "pic");

        _ = await fixture.ScanAsync(CancellationToken);

        fixture.RuleWriter.Includes.Select(include => (include.Path.Value, include.Direction, include.RemoteId)).ShouldBe([("Photos", SyncDirectionFactory.CreateTwoWay(), AStarDev.FunctionalParadigm.Option.None<RemoteItemId>())]);
    }

    [Fact]
    public async Task when_a_new_folder_gets_its_rule_then_its_files_are_reported_in_the_same_scan()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Photos/a.jpg", "pic");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Added:Photos/a.jpg:File");
    }

    [Fact]
    public async Task when_the_account_has_no_rules_then_each_new_top_level_folder_gets_a_rule()
    {
        _ = fixture.Write("Photos/a.jpg", "pic");
        _ = fixture.Write("Music/b.mp3", "tune");

        _ = await fixture.ScanAsync(CancellationToken);

        Included().ShouldBe(["Music", "Photos"], ignoreOrder: true);
    }

    [Fact]
    public async Task when_the_folder_also_exists_remotely_then_it_stays_excluded_whatever_the_casing()
    {
        fixture.Include("Docs");
        fixture.Remote.Folder("photos");
        _ = fixture.Write("Photos/a.jpg", "pic");

        var set = await fixture.ScanAsync(CancellationToken);

        (Included(), set.Changes.Count).ShouldBe(([], 0));
    }

    [Fact]
    public async Task when_the_folder_sits_under_an_exclude_rule_then_it_gets_no_rule()
    {
        fixture.SetRules(SelectionTestRules.Include("Docs"), SelectionTestRules.Exclude("Docs/Private"));
        _ = fixture.Write("Docs/Private/Sub/a.txt", "secret");

        var set = await fixture.ScanAsync(CancellationToken);

        (Included(), set.Changes.Count).ShouldBe(([], 0));
    }

    [Fact]
    public async Task when_the_folder_is_covered_by_an_include_rule_then_no_rule_is_written()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Docs/New/a.txt", "hello");

        _ = await fixture.ScanAsync(CancellationToken);

        fixture.RuleWriter.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_folder_is_new_below_a_partly_included_parent_then_it_gets_a_rule_at_its_own_path()
    {
        fixture.Include("A/B");
        fixture.Remote.Folder("A");
        _ = fixture.Write("A/C/x.txt", "x");

        _ = await fixture.ScanAsync(CancellationToken);

        Included().ShouldBe(["A/C"]);
    }

    [Fact]
    public async Task when_a_new_folder_has_new_folders_inside_then_only_the_top_most_gets_a_rule()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Photos/2026/Trip/a.jpg", "pic");

        _ = await fixture.ScanAsync(CancellationToken);

        Included().ShouldBe(["Photos"]);
    }

    [Fact]
    public async Task when_the_remote_index_has_no_cursor_yet_then_no_rule_is_written()
    {
        fixture.Include("Docs");
        fixture.Remote.Cursor = AStarDev.FunctionalParadigm.Option.None<ChangeCursor>();
        _ = fixture.Write("Photos/a.jpg", "pic");

        var set = await fixture.ScanAsync(CancellationToken);

        (fixture.RuleWriter.Calls, set.Changes.Count).ShouldBe((0, 0));
    }

    [Fact]
    public async Task when_the_remote_index_cannot_be_read_then_no_rule_is_written_and_the_scan_goes_on()
    {
        fixture.Include("Docs");
        fixture.Remote.Fails = true;
        _ = fixture.Write("Photos/a.jpg", "pic");
        _ = fixture.Write("Docs/new.txt", "hello");

        var set = await fixture.ScanAsync(CancellationToken);

        (fixture.RuleWriter.Calls, LocalScanFixture.Describe(set)).ShouldBe((0, "Added:Docs/new.txt:File"));
    }

    [Fact]
    public async Task when_the_rule_cannot_be_saved_then_the_scan_goes_on_without_the_folder_and_logs_no_name()
    {
        fixture.Include("Docs");
        fixture.RuleWriter.Fails = true;
        _ = fixture.Write("Photos/a.jpg", "pic");
        _ = fixture.Write("Docs/new.txt", "hello");

        var set = await fixture.ScanAsync(CancellationToken);

        (LocalScanFixture.Describe(set), fixture.Logs.Logger.Messages.Any(message => message.Contains("Photos", StringComparison.Ordinal))).ShouldBe(("Added:Docs/new.txt:File", false));
    }

    [Fact]
    public async Task when_two_new_folders_differ_only_in_case_then_one_rule_is_written()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Photos/a.jpg", "pic");
        _ = fixture.Write("photos/b.jpg", "pic");

        _ = await fixture.ScanAsync(CancellationToken);

        Included().Select(path => path.ToUpperInvariant()).ShouldBe(["PHOTOS"]);
    }

    [Fact]
    public async Task when_the_scan_runs_twice_then_the_rule_is_written_once()
    {
        fixture.Include("Docs");
        _ = fixture.Write("Photos/a.jpg", "pic");

        _ = await fixture.ScanAsync(CancellationToken);
        _ = await fixture.ScanAsync(CancellationToken);

        (fixture.RuleWriter.Calls, Included()).ShouldBe((1, ["Photos"]));
    }

    [Fact]
    public async Task when_the_new_folder_is_dot_prefixed_then_it_gets_no_rule()
    {
        fixture.Include("Docs");
        _ = fixture.Write(".cache/a.bin", "data");

        _ = await fixture.ScanAsync(CancellationToken);

        fixture.RuleWriter.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_new_folder_cannot_be_listed_then_it_gets_no_rule_and_the_scan_goes_on()
    {
        fixture.Include("Docs");
        fixture.Folder("Locked");
        fixture.Deny("Locked");
        _ = fixture.Write("Docs/new.txt", "hello");

        var set = await fixture.ScanAsync(CancellationToken);

        (Included().Length, LocalScanFixture.Describe(set)).ShouldBe((1, "Added:Docs/new.txt:File"));
    }
}
