using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Data.Sqlite;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenARemoteIndexStore : IAsyncLifetime
{
    private const string Modified = "2026-11-01T10:00:00Z";

    private static readonly AccountId FirstAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId SecondAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync() => database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    [Fact]
    public async Task when_no_page_was_applied_then_there_is_no_cursor_no_resync_and_no_item()
    {
        var cursor = await Database.IndexReader.GetCursorAsync(FirstAccount, CancellationToken);
        var pass = await Database.IndexReader.GetResyncPassAsync(FirstAccount, CancellationToken);
        var item = await Database.IndexReader.GetAsync(FirstAccount, Id("x"), CancellationToken);

        (cursor.Match(found => found.Match(_ => true, () => false), _ => true), pass.Match(found => found.Match(_ => true, () => false), _ => true), item.Match(found => found.Match(_ => true, () => false), _ => true)).ShouldBe((false, false, false));
    }

    [Fact]
    public async Task when_a_page_is_applied_then_its_items_are_added_and_its_cursor_is_saved()
    {
        var applied = await Apply(FirstAccount, More("c1", Folder("a", "root-id", "Docs"), File("a1", "a", "one.txt")));

        (Describe(applied), await CursorOf(FirstAccount)).ShouldBe(("Added:a,Added:a1", "c1"));
    }

    [Fact]
    public async Task when_the_same_page_is_applied_again_then_nothing_changes()
    {
        var page = More("c1", Folder("a", "root-id", "Docs"), File("a1", "a", "one.txt"));
        _ = await Apply(FirstAccount, page);

        var again = await Database.IndexWriter.ApplyPageAsync(FirstAccount, page, CancellationToken);

        (Describe(again), (await Children(FirstAccount, Option.Some(Id("a")))).Count).ShouldBe(("", 1));
    }

    [Fact]
    public async Task when_an_item_appears_twice_in_a_page_then_the_last_occurrence_wins()
    {
        var applied = await Apply(FirstAccount, More("c1", File("f", "root-id", "old.txt"), File("f", "root-id", "new.txt")));

        (Describe(applied), (await Item(FirstAccount, "f")).Match(found => found.Name, () => string.Empty)).ShouldBe(("Added:f", "new.txt"));
    }

    [Fact]
    public async Task when_a_folder_is_renamed_then_one_moved_change_is_reported_and_its_descendants_get_the_new_path_without_being_rewritten()
    {
        _ = await Apply(FirstAccount, More("c1", Folder("a", "root-id", "Docs"), Folder("b", "a", "Work"), File("f", "b", "one.txt")));
        var before = await PathOf(FirstAccount, "f");

        var applied = await Apply(FirstAccount, More("c2", Folder("a", "root-id", "Papers")));

        (before, Describe(applied), await PathOf(FirstAccount, "f")).ShouldBe(("Docs/Work/one.txt", "Moved:a", "Papers/Work/one.txt"));
    }

    [Fact]
    public async Task when_an_item_changes_parent_then_the_moved_change_carries_the_old_parent_and_name()
    {
        _ = await Apply(FirstAccount, More("c1", Folder("a", "root-id", "Docs"), Folder("b", "root-id", "Work"), File("f", "a", "one.txt")));

        var applied = await Apply(FirstAccount, More("c2", File("f", "b", "two.txt")));

        var moved = applied.Match(delta => delta.Changes.OfType<RemoteChange.Moved>().Single(), _ => throw new InvalidOperationException("The page must apply."));
        (moved.OldParentId.Match(parent => parent.Value, () => string.Empty), moved.OldName, moved.Item.Name, await PathOf(FirstAccount, "f")).ShouldBe(("a", "one.txt", "two.txt", "Work/two.txt"));
    }

    [Fact]
    public async Task when_only_the_entity_tag_changes_then_no_change_is_reported_but_the_new_tag_is_stored()
    {
        _ = await Apply(FirstAccount, More("c1", File("f", "root-id", "one.txt", cTag: "c-1", eTag: "e-1")));

        var applied = await Apply(FirstAccount, More("c2", File("f", "root-id", "one.txt", cTag: "c-1", eTag: "e-2")));

        (Describe(applied), (await Item(FirstAccount, "f")).Match(found => found.ETag.Value, () => string.Empty)).ShouldBe(("", "e-2"));
    }

    [Theory]
    [InlineData("c-2", "hash-1")]
    [InlineData("c-1", "hash-2")]
    public async Task when_the_content_tag_or_the_hash_changes_then_a_modified_change_is_reported(string cTag, string hash)
    {
        _ = await Apply(FirstAccount, More("c1", File("f", "root-id", "one.txt", cTag: "c-1", hash: "hash-1")));

        var applied = await Apply(FirstAccount, More("c2", File("f", "root-id", "one.txt", cTag: cTag, hash: hash, eTag: "e-2")));

        Describe(applied).ShouldBe("Modified:f");
    }

    [Fact]
    public async Task when_a_file_is_deleted_then_a_deleted_change_is_reported_the_item_reads_back_as_deleted_and_it_leaves_the_children()
    {
        _ = await Apply(FirstAccount, More("c1", Folder("a", "root-id", "Docs"), File("f", "a", "one.txt")));

        var applied = await Apply(FirstAccount, More("c2", Gone("f", "a")));

        var children = await Children(FirstAccount, Option.Some(Id("a")));
        (Describe(applied), (await Item(FirstAccount, "f")).Match(found => found.Kind is RemoteItemKind.Deleted, () => false), children.Count).ShouldBe(("Deleted:f", true, 0));
    }

    [Fact]
    public async Task when_a_folder_is_deleted_without_its_children_in_the_feed_then_every_indexed_descendant_is_deleted_too()
    {
        _ = await Apply(FirstAccount, More("c1", Folder("a", "root-id", "Docs"), Folder("b", "a", "Work"), File("f", "b", "one.txt"), File("g", "a", "two.txt"), File("other", "root-id", "keep.txt")));

        var applied = await Apply(FirstAccount, More("c2", Gone("a", "root-id")));

        (DescribeSorted(applied), string.Join(',', (await Children(FirstAccount, Option.None<RemoteItemId>())).Select(found => found.Id.Value))).ShouldBe(("Deleted:a,Deleted:b,Deleted:f,Deleted:g", "other"));
    }

    [Fact]
    public async Task when_a_child_is_moved_out_of_a_deleted_folder_in_the_same_page_then_the_child_survives()
    {
        _ = await Apply(FirstAccount, More("c1", Folder("a", "root-id", "Docs"), Folder("b", "root-id", "Work"), File("f", "a", "one.txt")));

        var applied = await Apply(FirstAccount, More("c2", Gone("a", "root-id"), File("f", "b", "one.txt")));

        (DescribeSorted(applied), (await Item(FirstAccount, "f")).Match(found => found.Kind is RemoteItemKind.File, () => false)).ShouldBe(("Deleted:a,Moved:f", true));
    }

    [Fact]
    public async Task when_a_deleted_item_returns_then_it_is_added_again()
    {
        _ = await Apply(FirstAccount, More("c1", File("f", "root-id", "one.txt")));
        _ = await Apply(FirstAccount, More("c2", Gone("f", "root-id")));

        var applied = await Apply(FirstAccount, More("c3", File("f", "root-id", "one.txt")));

        (Describe(applied), (await Children(FirstAccount, Option.None<RemoteItemId>())).Count).ShouldBe(("Added:f", 1));
    }

    [Fact]
    public async Task when_the_top_level_is_read_then_items_under_the_unindexed_root_come_back_sorted_ignoring_case()
    {
        _ = await Apply(FirstAccount, More("c1", File("z", "root-id", "zeta.txt"), File("a", "root-id", "Alpha.txt"), File("b", "root-id", "beta.txt"), File("c", "a", "nested.txt")));

        var children = await Children(FirstAccount, Option.None<RemoteItemId>());

        children.Select(found => found.Name).ToList().ShouldBe(["Alpha.txt", "beta.txt", "zeta.txt"]);
    }

    [Fact]
    public async Task when_an_item_is_unknown_or_its_parent_chain_loops_then_it_has_no_path()
    {
        _ = await Apply(FirstAccount, More("c1", Folder("a", "b", "One"), Folder("b", "a", "Two")));

        (await PathOf(FirstAccount, "a"), await PathOf(FirstAccount, "missing")).ShouldBe((string.Empty, string.Empty));
    }

    [Fact]
    public async Task when_two_accounts_hold_the_same_item_id_then_they_are_kept_apart()
    {
        _ = await Apply(FirstAccount, More("c1", File("f", "root-id", "mine.txt")));
        _ = await Apply(SecondAccount, More("c9", File("f", "root-id", "yours.txt")));

        ((await Item(FirstAccount, "f")).Match(found => found.Name, () => string.Empty), (await Item(SecondAccount, "f")).Match(found => found.Name, () => string.Empty), await CursorOf(FirstAccount), await CursorOf(SecondAccount)).ShouldBe(("mine.txt", "yours.txt", "c1", "c9"));
    }

    [Fact]
    public async Task when_an_account_is_removed_then_its_index_and_cursor_are_gone_and_the_others_stay()
    {
        _ = await Apply(FirstAccount, More("c1", File("f", "root-id", "mine.txt")));
        _ = await Apply(SecondAccount, More("c9", File("f", "root-id", "yours.txt")));

        var removed = await Database.IndexParticipant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        (removed.Match(_ => true, _ => false), (await Item(FirstAccount, "f")).Match(_ => true, () => false), await CursorOf(FirstAccount), (await Item(SecondAccount, "f")).Match(_ => true, () => false), await CursorOf(SecondAccount)).ShouldBe((true, false, string.Empty, true, "c9"));
    }

    [Fact]
    public async Task when_the_cursor_cannot_be_saved_then_the_page_is_not_applied_and_the_old_cursor_stays()
    {
        _ = await Apply(FirstAccount, More("c1", File("f", "root-id", "one.txt")));
        Execute("CREATE TRIGGER block_cursor BEFORE UPDATE ON RemoteCursors BEGIN SELECT RAISE(ABORT, 'blocked'); END;");

        var applied = await Apply(FirstAccount, More("c2", File("g", "root-id", "two.txt"), File("f", "root-id", "renamed.txt")));

        (applied.Match(_ => true, _ => false), await CursorOf(FirstAccount), (await Item(FirstAccount, "g")).Match(_ => true, () => false), (await Item(FirstAccount, "f")).Match(found => found.Name, () => string.Empty)).ShouldBe((false, "c1", false, "one.txt"));
    }

    [Fact]
    public async Task when_a_page_fails_then_it_can_be_applied_again_once_the_fault_is_gone()
    {
        Execute("CREATE TRIGGER block_cursor BEFORE INSERT ON RemoteCursors BEGIN SELECT RAISE(ABORT, 'blocked'); END;");
        var page = More("c1", File("f", "root-id", "one.txt"));
        var failed = await Database.IndexWriter.ApplyPageAsync(FirstAccount, page, CancellationToken);
        Execute("DROP TRIGGER block_cursor;");

        var retried = await Database.IndexWriter.ApplyPageAsync(FirstAccount, page, CancellationToken);

        (failed.Match(_ => true, _ => false), Describe(retried), await CursorOf(FirstAccount)).ShouldBe((false, "Added:f", "c1"));
    }

    [Fact]
    public async Task when_a_save_fails_then_the_logs_carry_no_names_or_cursors()
    {
        Execute("CREATE TRIGGER block_cursor BEFORE INSERT ON RemoteCursors BEGIN SELECT RAISE(ABORT, 'blocked'); END;");

        _ = await Apply(FirstAccount, More("secret-cursor", File("f", "root-id", "Taxes 2026.txt")));

        var everything = string.Join('\n', Database.Logs.Logger.Messages);
        (everything.Contains("Taxes", StringComparison.Ordinal), everything.Contains("secret-cursor", StringComparison.Ordinal), everything.Contains(FirstAccount.Value.ToString(), StringComparison.OrdinalIgnoreCase)).ShouldBe((false, false, false));
    }

    [Fact]
    public async Task when_twenty_pages_that_touch_the_same_item_are_applied_at_once_then_every_one_succeeds_and_one_row_remains()
    {
        var start = new TaskCompletionSource();
        var pending = Enumerable.Range(0, 20).Select(number => Task.Run(async () => await start.Task.ContinueWith(_ => Apply(FirstAccount, More($"c{number}", File("shared", "root-id", $"name-{number}.txt"), File($"own-{number}", "root-id", $"own-{number}.txt"))), CancellationToken).Unwrap(), CancellationToken)).ToList();

        start.SetResult();
        var results = await Task.WhenAll(pending);

        results.ShouldAllBe(result => result.Match(_ => true, _ => false));
        (await Children(FirstAccount, Option.None<RemoteItemId>())).Count.ShouldBe(21);
    }

    [Fact]
    public async Task when_a_resync_runs_then_items_no_page_returned_are_deleted_and_the_pass_ends()
    {
        _ = await Apply(FirstAccount, More("c1", File("keep", "root-id", "keep.txt"), File("gone", "root-id", "gone.txt")));
        var pass = await Database.IndexWriter.BeginResyncAsync(FirstAccount, CancellationToken);
        _ = await Apply(FirstAccount, Caught("c2", File("keep", "root-id", "keep.txt")));

        var completed = await Database.IndexWriter.CompleteResyncAsync(FirstAccount, CancellationToken);

        var running = await Database.IndexReader.GetResyncPassAsync(FirstAccount, CancellationToken);
        (pass.Match(_ => true, _ => false), Describe(completed), running.Match(found => found.Match(_ => true, () => false), _ => true), string.Join(',', (await Children(FirstAccount, Option.None<RemoteItemId>())).Select(found => found.Id.Value))).ShouldBe((true, "Deleted:gone", false, "keep"));
    }

    [Fact]
    public async Task when_a_resync_is_begun_again_before_it_ends_then_the_same_pass_resumes()
    {
        _ = await Apply(FirstAccount, More("c1", File("f", "root-id", "one.txt")));
        var first = await Database.IndexWriter.BeginResyncAsync(FirstAccount, CancellationToken);
        _ = await Apply(FirstAccount, More("c2", File("f", "root-id", "one.txt")));

        var second = await Database.IndexWriter.BeginResyncAsync(FirstAccount, CancellationToken);
        var reading = await Database.IndexReader.GetResyncPassAsync(FirstAccount, CancellationToken);

        (second.Match(found => found.Value, _ => Guid.Empty), reading.Match(found => found.Match(pass => pass.Value, () => Guid.Empty), _ => Guid.Empty)).ShouldBe((first.Match(found => found.Value, _ => Guid.Empty), first.Match(found => found.Value, _ => Guid.Empty)));
    }

    [Fact]
    public async Task when_a_resync_ends_without_having_begun_then_nothing_is_deleted()
    {
        _ = await Apply(FirstAccount, More("c1", File("f", "root-id", "one.txt")));

        var completed = await Database.IndexWriter.CompleteResyncAsync(FirstAccount, CancellationToken);

        (Describe(completed), (await Children(FirstAccount, Option.None<RemoteItemId>())).Count).ShouldBe(("", 1));
    }

    [Fact]
    public async Task when_three_hundred_thousand_items_arrive_in_pages_then_all_are_indexed_one_page_at_a_time()
    {
        const int pages = 60;
        const int pageSize = 5000;

        foreach (var number in Enumerable.Range(0, pages))
            _ = await Apply(FirstAccount, number == pages - 1 ? Caught($"c{number}", Items(number, pageSize)) : More($"c{number}", Items(number, pageSize)));

        (await CursorOf(FirstAccount), CountRows("RemoteItems")).ShouldBe(($"c{pages - 1}", pages * pageSize));
    }

    private static RemoteItemSnapshot[] Items(int pageNumber, int pageSize) => [.. Enumerable.Range(0, pageSize).Select(number => File($"item-{pageNumber}-{number}", "root-id", $"file-{pageNumber}-{number}.txt"))];

    private static RemoteItemId Id(string value) => RemoteItemIdFactory.Create(value).Match(id => id, _ => throw new InvalidOperationException("The test id must be valid."));

    private static ChangeCursor Cursor(string value) => ChangeCursorFactory.Create(value).Match(cursor => cursor, _ => throw new InvalidOperationException("The test cursor must be valid."));

    private static RemoteItemSnapshot Folder(string id, string parent, string name) =>
        RemoteItemSnapshotFactory.Create(Id(id), Option.Some(Id(parent)), name, RemoteItemKindFactory.CreateFolder(), DateTimeOffset.Parse(Modified, System.Globalization.CultureInfo.InvariantCulture), ETag($"e-{id}"));

    private static RemoteItemSnapshot File(string id, string parent, string name, string cTag = "c-1", string hash = "hash-1", string? eTag = null) =>
        RemoteItemSnapshotFactory.Create(Id(id), Option.Some(Id(parent)), name, RemoteItemKindFactory.CreateFile(10, HashOf(hash), Tag(cTag)), DateTimeOffset.Parse(Modified, System.Globalization.CultureInfo.InvariantCulture), ETag(eTag ?? $"e-{id}"));

    private static RemoteItemSnapshot Gone(string id, string parent) =>
        RemoteItemSnapshotFactory.Create(Id(id), Option.Some(Id(parent)), "gone", RemoteItemKindFactory.CreateDeleted(), DateTimeOffset.Parse(Modified, System.Globalization.CultureInfo.InvariantCulture), ETag($"e-{id}"));

    private static Option<ContentHash> HashOf(string value) => ContentHashFactory.Create(value).Match(hash => Option.Some(hash), _ => Option.None<ContentHash>());

    private static ContentTag Tag(string value) => ContentTagFactory.Create(value).Match(tag => tag, _ => throw new InvalidOperationException("The test tag must be valid."));

    private static EntityTag ETag(string value) => EntityTagFactory.Create(value).Match(tag => tag, _ => throw new InvalidOperationException("The test tag must be valid."));

    private static RemoteChangePage More(string cursor, params RemoteItemSnapshot[] items) => RemoteChangePageFactory.Create(items, PageContinuationFactory.CreateMorePages(Cursor(cursor)));

    private static RemoteChangePage Caught(string cursor, params RemoteItemSnapshot[] items) => RemoteChangePageFactory.Create(items, PageContinuationFactory.CreateCaughtUp(Cursor(cursor)));

    private static string Describe(Result<RemoteIndexDelta, ErrorResponse> result) => string.Join(',', Changes(result));

    private static string DescribeSorted(Result<RemoteIndexDelta, ErrorResponse> result) => string.Join(',', Changes(result).Order(StringComparer.Ordinal));

    private static IEnumerable<string> Changes(Result<RemoteIndexDelta, ErrorResponse> result) => result.Match(
        delta => delta.Changes.Select(change => change switch
        {
            RemoteChange.Added added => $"Added:{added.Item.Id.Value}",
            RemoteChange.Modified modified => $"Modified:{modified.Item.Id.Value}",
            RemoteChange.Moved moved => $"Moved:{moved.Item.Id.Value}",
            RemoteChange.Deleted deleted => $"Deleted:{deleted.Id.Value}",
            _ => "unknown",
        }),
        error => [$"error:{error.Message}"]);

    private Task<Result<RemoteIndexDelta, ErrorResponse>> Apply(AccountId account, RemoteChangePage page) => Database.IndexWriter.ApplyPageAsync(account, page, CancellationToken);

    private async Task<string> CursorOf(AccountId account) => (await Database.IndexReader.GetCursorAsync(account, CancellationToken)).Match(found => found.Match(cursor => cursor.Value, () => string.Empty), _ => "error");

    private async Task<Option<RemoteItemSnapshot>> Item(AccountId account, string id) => (await Database.IndexReader.GetAsync(account, Id(id), CancellationToken)).Match(found => found, _ => Option.None<RemoteItemSnapshot>());

    private async Task<IReadOnlyList<RemoteItemSnapshot>> Children(AccountId account, Option<RemoteItemId> parent) => (await Database.IndexReader.GetChildrenAsync(account, parent, CancellationToken)).Match(found => found, _ => []);

    private async Task<string> PathOf(AccountId account, string id) => (await Database.IndexReader.GetPathAsync(account, Id(id), CancellationToken)).Match(found => found.Match(path => path.Value ?? string.Empty, () => string.Empty), _ => "error");

    private void Execute(string sql)
    {
        using var connection = new SqliteConnection($"Data Source={Database.DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        _ = command.ExecuteNonQuery();
    }

    private long CountRows(string table)
    {
        using var connection = new SqliteConnection($"Data Source={Database.DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table}";

        return (long)command.ExecuteScalar()!;
    }
}
