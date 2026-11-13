using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using static AStarDev.CloudSync.Sync.Sqlite.TestsIntegration.RemoteIndexData;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenARemoteIndexUpdater : IAsyncLifetime
{
    private readonly ScriptedChangeFeed feed = new();

    private TemporarySyncDatabase? database;
    private AccountId account;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    private IRemoteIndexUpdater Updater => Database.Services.GetRequiredService<IRemoteIndexUpdater>();

    public async ValueTask InitializeAsync()
    {
        database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken, services => services.AddCloudSync().AddKeyedSingleton<IRemoteChangeFeed>(CloudProviderFactory.CreateOneDrive(), feed));
        var key = ProviderAccountKeyFactory.Create("key-1").Match(created => created, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(key, CloudProviderFactory.CreateOneDrive(), "Personal", "personal@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1, 2));
        account = (await Database.AccountStore.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    [Fact]
    public async Task when_the_feed_has_three_pages_then_the_updater_reads_until_caught_up_and_saves_the_last_cursor()
    {
        feed.Enqueue(More("c1", File("f1", "root-id", "one.txt")));
        feed.Enqueue(More("c2", File("f2", "root-id", "two.txt")));
        feed.Enqueue(Caught("c3", File("f3", "root-id", "three.txt")));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        (Describe(summary), string.Join(',', feed.Requested), await CursorOf()).ShouldBe(("0/0/0/0 pages=3 resync=none rules=0/0", "none,c1,c2", "c3"));
    }

    [Fact]
    public async Task when_an_update_starts_then_it_continues_from_the_saved_cursor()
    {
        feed.Enqueue(Caught("c1", File("f1", "root-id", "one.txt")));
        _ = await Updater.UpdateAsync(account, CancellationToken);
        feed.Enqueue(Caught("c2"));

        _ = await Updater.UpdateAsync(account, CancellationToken);

        string.Join(',', feed.Requested).ShouldBe("none,c1");
    }

    [Fact]
    public async Task when_changes_sit_inside_or_outside_the_selected_folder_then_only_the_ones_inside_count()
    {
        await Include("Docs", "a");
        feed.Enqueue(Caught("c1", Folder("a", "root-id", "Docs"), File("f", "a", "one.txt"), File("g", "root-id", "outside.txt")));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        Describe(summary).ShouldBe("2/0/0/0 pages=1 resync=none rules=0/0");
    }

    [Fact]
    public async Task when_no_folder_is_selected_then_nothing_is_relevant()
    {
        feed.Enqueue(Caught("c1", Folder("a", "root-id", "Docs"), File("f", "a", "one.txt")));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        Describe(summary).ShouldBe("0/0/0/0 pages=1 resync=none rules=0/0");
    }

    [Fact]
    public async Task when_items_move_into_out_of_and_inside_the_selection_then_they_count_as_add_delete_and_move()
    {
        await Include("Docs", "a");
        feed.Enqueue(Caught("c1", Folder("a", "root-id", "Docs"), Folder("b", "root-id", "Other"), File("f", "a", "in.txt"), File("h", "b", "out.txt"), File("m", "a", "stay.txt")));
        _ = await Updater.UpdateAsync(account, CancellationToken);
        feed.Enqueue(Caught("c2", File("f", "b", "in.txt"), File("h", "a", "out.txt"), File("m", "a", "renamed.txt")));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        Describe(summary).ShouldBe("1/0/1/1 pages=1 resync=none rules=0/0");
    }

    [Fact]
    public async Task when_the_content_of_a_selected_file_changes_then_a_modified_change_counts()
    {
        await Include("Docs", "a");
        feed.Enqueue(Caught("c1", Folder("a", "root-id", "Docs"), File("f", "a", "one.txt", cTag: "c-1")));
        _ = await Updater.UpdateAsync(account, CancellationToken);
        feed.Enqueue(Caught("c2", File("f", "a", "one.txt", cTag: "c-2", eTag: "e-2")));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        Describe(summary).ShouldBe("0/1/0/0 pages=1 resync=none rules=0/0");
    }

    [Fact]
    public async Task when_a_selected_folder_is_renamed_then_its_rules_follow_it_and_the_update_counts_one_move()
    {
        await Include("Docs", "a");
        await Include("Docs/Work", "c");
        feed.Enqueue(Caught("c1", Folder("a", "root-id", "Docs"), Folder("c", "a", "Work")));
        _ = await Updater.UpdateAsync(account, CancellationToken);
        feed.Enqueue(Caught("c2", Folder("a", "root-id", "Papers")));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        (Describe(summary), await RulePaths()).ShouldBe(("0/0/1/0 pages=1 resync=none rules=2/0", "Papers,Papers/Work"));
    }

    [Fact]
    public async Task when_the_new_path_already_has_a_rule_then_the_existing_rule_wins_and_the_moved_rule_is_dropped()
    {
        await Include("Docs", "a");
        _ = await Database.RuleWriter.ApplyAsync(account, [SelectionChangeFactory.CreateExcludeFolder(Path("Papers"), Option.None<RemoteItemId>())], CancellationToken);
        feed.Enqueue(Caught("c1", Folder("a", "root-id", "Docs")));
        _ = await Updater.UpdateAsync(account, CancellationToken);
        feed.Enqueue(Caught("c2", Folder("a", "root-id", "Papers")));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        (Describe(summary), await RulePaths()).ShouldBe(("0/0/0/1 pages=1 resync=none rules=0/1", "Papers"));
    }

    [Fact]
    public async Task when_two_folders_swap_names_then_both_rules_follow_their_folders_and_none_is_dropped()
    {
        await Include("A", "a");
        await Include("B", "b");
        feed.Enqueue(Caught("c1", Folder("a", "root-id", "A"), Folder("b", "root-id", "B")));
        _ = await Updater.UpdateAsync(account, CancellationToken);
        feed.Enqueue(Caught("c2", Folder("a", "root-id", "B"), Folder("b", "root-id", "A")));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        (Describe(summary), await RulesWithRemoteIds()).ShouldBe(("0/0/2/0 pages=1 resync=none rules=2/0", "A=b,B=a"));
    }

    [Fact]
    public async Task when_one_folder_takes_the_old_name_of_another_that_is_renamed_then_neither_rule_is_dropped()
    {
        await Include("A", "a");
        await Include("B", "b");
        feed.Enqueue(Caught("c1", Folder("a", "root-id", "A"), Folder("b", "root-id", "B")));
        _ = await Updater.UpdateAsync(account, CancellationToken);
        feed.Enqueue(Caught("c2", Folder("a", "root-id", "B"), Folder("b", "root-id", "C")));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        (Describe(summary), await RulesWithRemoteIds()).ShouldBe(("0/0/2/0 pages=1 resync=none rules=2/0", "B=a,C=b"));
    }

    [Fact]
    public async Task when_a_page_was_saved_but_its_rules_were_never_relocated_then_the_next_update_relocates_them()
    {
        await Include("Docs", "a");
        feed.Enqueue(Caught("c1", Folder("a", "root-id", "Docs")));
        _ = await Updater.UpdateAsync(account, CancellationToken);
        _ = await Database.IndexWriter.ApplyPageAsync(account, Caught("c2", Folder("a", "root-id", "Papers")), CancellationToken);
        feed.Enqueue(Caught("c3"));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        (Describe(summary), await RulePaths()).ShouldBe(("0/0/0/0 pages=1 resync=none rules=1/0", "Papers"));
    }

    [Fact]
    public async Task when_only_deletions_arrive_then_the_summary_reports_them()
    {
        await Include("Docs", "a");
        feed.Enqueue(Caught("c1", Folder("a", "root-id", "Docs"), File("f", "a", "one.txt")));
        _ = await Updater.UpdateAsync(account, CancellationToken);
        feed.Enqueue(Caught("c2", Gone("f", "a")));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        (Describe(summary), summary.Match(found => found.Changes.Total(), _ => -1)).ShouldBe(("0/0/0/1 pages=1 resync=none rules=0/0", 1));
    }

    [Fact]
    public async Task when_the_cursor_expired_then_the_updater_resyncs_and_reports_items_the_feed_no_longer_returns_as_deleted()
    {
        await Include("Docs", "a");
        feed.Enqueue(Caught("c1", Folder("a", "root-id", "Docs"), File("keep", "a", "keep.txt"), File("gone", "a", "gone.txt")));
        _ = await Updater.UpdateAsync(account, CancellationToken);
        feed.Enqueue(ProviderErrorFactory.CreateCursorExpired(ResyncKindFactory.CreateApplyDifferences(), Cursor("r1")));
        feed.Enqueue(Caught("c2", Folder("a", "root-id", "Docs"), File("keep", "a", "keep.txt")));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        var gone = (await Database.IndexReader.GetAsync(account, Id("gone"), CancellationToken)).Match(found => found.Match(item => item.Kind is RemoteItemKind.Deleted, () => false), _ => false);
        var pass = (await Database.IndexReader.GetResyncPassAsync(account, CancellationToken)).Match(found => found.Match(_ => true, () => false), _ => true);
        (Describe(summary), string.Join(',', feed.Requested), gone, pass, await CursorOf()).ShouldBe(("0/0/0/1 pages=1 resync=ApplyDifferences rules=0/0", "none,c1,r1", true, false, "c2"));
    }

    [Fact]
    public async Task when_a_resync_was_interrupted_then_the_next_update_resumes_it_and_finishes_it()
    {
        await Include("Docs", "a");
        feed.Enqueue(Caught("c1", Folder("a", "root-id", "Docs"), File("keep", "a", "keep.txt"), File("gone", "a", "gone.txt")));
        _ = await Updater.UpdateAsync(account, CancellationToken);
        _ = await Database.IndexWriter.BeginResyncAsync(account, CancellationToken);
        _ = await Database.IndexWriter.ApplyPageAsync(account, More("c-mid", Folder("a", "root-id", "Docs"), File("keep", "a", "keep.txt")), CancellationToken);
        feed.Enqueue(Caught("c2"));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        (Describe(summary), string.Join(',', feed.Requested)).ShouldBe(("0/0/0/1 pages=1 resync=Unknown rules=0/0", "none,c-mid"));
    }

    [Fact]
    public async Task when_the_cursor_expires_again_during_the_resync_then_the_expiry_is_returned_instead_of_looping()
    {
        feed.Enqueue(Caught("c1", File("f", "root-id", "one.txt")));
        _ = await Updater.UpdateAsync(account, CancellationToken);
        feed.Enqueue(ProviderErrorFactory.CreateCursorExpired(ResyncKindFactory.CreateUnknown(), Cursor("r1")));
        feed.Enqueue(ProviderErrorFactory.CreateCursorExpired(ResyncKindFactory.CreateUnknown(), Cursor("r2")));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        (summary.Match(_ => "summary", error => error.GetType().Name), string.Join(',', feed.Requested)).ShouldBe(("CursorExpired", "none,c1,r1"));
    }

    [Fact]
    public async Task when_the_feed_returns_the_same_link_again_then_the_update_stops_as_unexpected()
    {
        feed.Enqueue(More("c1", File("f", "root-id", "one.txt")));
        feed.Enqueue(More("c1", File("g", "root-id", "two.txt")));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        (summary.Match(_ => "summary", error => error.GetType().Name), string.Join(',', feed.Requested)).ShouldBe(("Unexpected", "none,c1"));
    }

    [Fact]
    public async Task when_the_feed_fails_after_a_page_then_the_error_is_returned_and_the_saved_cursor_stays_on_that_page()
    {
        feed.Enqueue(More("c1", File("f", "root-id", "one.txt")));
        feed.Enqueue(ProviderErrorFactory.CreateThrottled(Option.None<TimeSpan>()));

        var summary = await Updater.UpdateAsync(account, CancellationToken);

        (summary.Match(_ => "summary", error => error.GetType().Name), await CursorOf()).ShouldBe(("Throttled", "c1"));
    }

    [Fact]
    public async Task when_the_caller_cancels_during_a_page_then_the_update_is_cancelled_and_the_last_saved_cursor_is_kept()
    {
        feed.Enqueue(Caught("c1", File("f", "root-id", "one.txt")));
        _ = await Updater.UpdateAsync(account, CancellationToken);
        using var cancelled = new CancellationTokenSource();
        feed.Enqueue(More("c2", File("g", "root-id", "two.txt")), cancelled.Cancel);

        var updating = Updater.UpdateAsync(account, cancelled.Token);

        await Should.ThrowAsync<OperationCanceledException>(updating);
        (await CursorOf()).ShouldBe("c1");
    }

    [Fact]
    public async Task when_the_account_is_unknown_then_not_found_is_returned()
    {
        var summary = await Updater.UpdateAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        summary.Match(_ => "summary", error => error.GetType().Name).ShouldBe("NotFound");
    }

    [Fact]
    public async Task when_updates_for_one_account_run_at_once_then_only_one_reads_the_feed_at_a_time()
    {
        foreach (var number in Enumerable.Range(0, 6))
            feed.Enqueue(Caught($"c{number}"));
        var start = new TaskCompletionSource();
        var pending = Enumerable.Range(0, 6).Select(_ => Task.Run(async () => await start.Task.ContinueWith(_ => Updater.UpdateAsync(account, CancellationToken), CancellationToken).Unwrap(), CancellationToken)).ToList();

        start.SetResult();
        var results = await Task.WhenAll(pending);

        (results.All(result => result.Match(_ => true, _ => false)), feed.MostRunningAtOnce).ShouldBe((true, 1));
    }

    [Fact]
    public async Task when_an_update_runs_then_the_logs_carry_counts_but_no_names_cursors_or_account_id()
    {
        await Include("Docs", "a");
        feed.Enqueue(Caught("secret-cursor", Folder("a", "root-id", "Docs"), File("f", "a", "Taxes 2026.txt")));

        _ = await Updater.UpdateAsync(account, CancellationToken);

        var everything = string.Join('\n', Database.Logs.Logger.Messages.Concat(Database.Logs.Logger.Scopes.SelectMany(scope => scope.Values.Select(value => value?.ToString() ?? string.Empty))));
        (everything.Contains("Taxes", StringComparison.Ordinal), everything.Contains("secret-cursor", StringComparison.Ordinal), everything.Contains(account.Value.ToString(), StringComparison.OrdinalIgnoreCase), everything.Contains("pages", StringComparison.OrdinalIgnoreCase)).ShouldBe((false, false, false, true));
    }

    [Fact]
    public async Task when_no_folder_moved_then_the_relocator_changes_nothing()
    {
        await Include("Docs", "a");

        var relocated = await Database.Services.GetRequiredService<ISelectionRuleRelocator>().RelocateAsync(account, CancellationToken);

        (relocated.Match(found => $"{found.Relocated}/{found.Dropped}", _ => "error"), await RulePaths()).ShouldBe(("0/0", "Docs"));
    }

    private static string Describe(Result<RemoteUpdateSummary, ProviderError> result) => result.Match(
        found => $"{found.Changes.Added}/{found.Changes.Modified}/{found.Changes.Moved}/{found.Changes.Deleted} pages={found.PagesRead} resync={found.Resync.Match(kind => kind.GetType().Name, () => "none")} rules={found.Rules.Relocated}/{found.Rules.Dropped}",
        error => $"error:{error.GetType().Name}");

    private static LocalRelativePath Path(string value) => LocalRelativePathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));

    private async Task Include(string path, string remoteId) =>
        _ = await Database.RuleWriter.ApplyAsync(account, [SelectionChangeFactory.CreateIncludeFolder(Path(path), Option.Some(Id(remoteId)), SyncDirectionFactory.CreateTwoWay())], CancellationToken);

    private async Task<string> RulePaths() => string.Join(',', (await Database.Rules.GetAsync(account, CancellationToken)).Match(found => found.Select(rule => rule.Path.Value), _ => []));

    private async Task<string> RulesWithRemoteIds() => string.Join(',', (await Database.Rules.GetAsync(account, CancellationToken)).Match(found => found.Select(rule => $"{rule.Path.Value}={rule.RemoteId.Match(id => id.Value, () => string.Empty)}"), _ => []));

    private async Task<string> CursorOf() => (await Database.IndexReader.GetCursorAsync(account, CancellationToken)).Match(found => found.Match(cursor => cursor.Value, () => string.Empty), _ => "error");
}
