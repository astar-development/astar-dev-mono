using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.Sqlite.TestsIntegration.RemoteIndexData;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenASyncBaselineStore : IAsyncLifetime
{
    private static readonly AccountId FirstAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId SecondAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateTimeOffset WrittenAt = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

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
    public async Task when_a_file_entry_is_saved_then_every_field_reads_back_exactly()
    {
        var entry = FileEntry(FirstAccount, "Docs/Report.docx", "item-1", size: 1234, hash: "h1", cTag: "c1", eTag: "e1");
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [entry], CancellationToken);

        var read = await Database.BaselineReader.GetAsync(FirstAccount, Path("docs/REPORT.docx"), CancellationToken);

        var found = read.Match(option => option.Match(value => value, () => throw new InvalidOperationException("The entry must exist.")), _ => throw new InvalidOperationException("The read must succeed."));
        (found.ItemId.Value, found.Kind).ShouldBe(("item-1", entry.Kind));
    }

    [Fact]
    public async Task when_a_folder_entry_is_saved_then_it_reads_back_as_a_folder()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FolderEntry(FirstAccount, "Docs", "folder-1")], CancellationToken);

        var read = await Database.BaselineReader.GetAsync(FirstAccount, Path("Docs"), CancellationToken);

        read.Match(option => option.Match(value => value.Kind is SyncBaselineKind.Folder, () => false), _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_path_was_never_synced_then_the_read_is_none()
    {
        var read = await Database.BaselineReader.GetAsync(FirstAccount, Path("Nothing/here.txt"), CancellationToken);

        read.Match(option => option.Match(_ => "found", () => "none"), _ => "failed").ShouldBe("none");
    }

    [Fact]
    public async Task when_an_entry_is_saved_again_with_a_casing_variant_then_the_row_is_updated_not_duplicated()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "Docs/Report.docx", "item-1", size: 1, hash: "h1")], CancellationToken);
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "docs/report.DOCX", "item-1", size: 2, hash: "h2")], CancellationToken);

        var page = await Page(FirstAccount, "", Option.None<RelativeSyncPath>(), 100);

        page.Select(entry => ((SyncBaselineKind.File)entry.Kind).Hash.Value).ShouldBe(["h2"]);
    }

    [Fact]
    public async Task when_one_save_holds_two_casing_variants_then_the_last_wins()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "A/b.txt", "item-1", hash: "first"), FileEntry(FirstAccount, "a/B.TXT", "item-1", hash: "last")], CancellationToken);

        var page = await Page(FirstAccount, "", Option.None<RelativeSyncPath>(), 100);

        page.Select(entry => ((SyncBaselineKind.File)entry.Kind).Hash.Value).ShouldBe(["last"]);
    }

    [Fact]
    public async Task when_a_folder_is_read_then_it_and_everything_below_it_come_back_but_not_a_sibling_sharing_its_prefix()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FolderEntry(FirstAccount, "Photos", "p"), FileEntry(FirstAccount, "Photos/a.jpg", "pa"), FileEntry(FirstAccount, "photos/2024/b.jpg", "pb"), FileEntry(FirstAccount, "Photos2/c.jpg", "pc"), FileEntry(FirstAccount, "Docs/d.txt", "pd")], CancellationToken);

        var page = await Page(FirstAccount, "PHOTOS", Option.None<RelativeSyncPath>(), 100);

        page.Select(entry => entry.ItemId.Value).ShouldBe(["p", "pb", "pa"]);
    }

    [Fact]
    public async Task when_the_whole_account_is_read_in_pages_then_every_entry_comes_back_once_in_path_order()
    {
        var entries = Enumerable.Range(0, 25).Select(index => FileEntry(FirstAccount, $"Docs/file{index:00}.txt", $"item-{index:00}")).ToList();
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, entries, CancellationToken);

        var seen = new List<string>();
        var after = Option.None<RelativeSyncPath>();
        var pages = 0;
        while (true)
        {
            var page = await Page(FirstAccount, "", after, 10);
            pages++;
            seen.AddRange(page.Select(entry => entry.ItemId.Value));
            if (page.Count < 10)
                break;

            after = Option.Some(page[^1].Path);
        }

        (pages, string.Join(',', seen)).ShouldBe((3, string.Join(',', entries.Select(entry => entry.ItemId.Value))));
    }

    [Fact]
    public async Task when_the_limit_is_below_one_then_one_entry_comes_back()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "a.txt", "1"), FileEntry(FirstAccount, "b.txt", "2")], CancellationToken);

        var page = await Page(FirstAccount, "", Option.None<RelativeSyncPath>(), 0);

        page.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_two_accounts_share_a_path_then_each_keeps_its_own_entry()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "Docs/a.txt", "first")], CancellationToken);
        _ = await Database.BaselineWriter.UpsertAsync(SecondAccount, [FileEntry(SecondAccount, "Docs/a.txt", "second")], CancellationToken);

        var first = await Page(FirstAccount, "", Option.None<RelativeSyncPath>(), 100);
        var second = await Page(SecondAccount, "", Option.None<RelativeSyncPath>(), 100);

        (first.Select(entry => entry.ItemId.Value).Single(), second.Select(entry => entry.ItemId.Value).Single()).ShouldBe(("first", "second"));
    }

    [Fact]
    public async Task when_entries_are_removed_then_only_those_paths_go_ignoring_case()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "Docs/a.txt", "1"), FileEntry(FirstAccount, "Docs/b.txt", "2")], CancellationToken);

        _ = await Database.BaselineWriter.RemoveAsync(FirstAccount, [Path("DOCS/A.TXT"), Path("Docs/never-synced.txt")], CancellationToken);

        (await Page(FirstAccount, "", Option.None<RelativeSyncPath>(), 100)).Select(entry => entry.ItemId.Value).ShouldBe(["2"]);
    }

    [Fact]
    public async Task when_the_account_is_removed_then_its_baseline_goes_and_another_accounts_stays()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "a.txt", "1")], CancellationToken);
        _ = await Database.BaselineWriter.UpsertAsync(SecondAccount, [FileEntry(SecondAccount, "a.txt", "2")], CancellationToken);

        _ = await Database.BaselineParticipant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        ((await Page(FirstAccount, "", Option.None<RelativeSyncPath>(), 100)).Count, (await Page(SecondAccount, "", Option.None<RelativeSyncPath>(), 100)).Count).ShouldBe((0, 1));
    }

    [Fact]
    public async Task when_many_passes_save_the_same_path_at_once_then_one_row_remains()
    {
        var saves = Enumerable.Range(0, 20).Select(index => Task.Run(() => Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, index % 2 == 0 ? "Docs/a.txt" : "DOCS/A.TXT", "item", hash: $"h{index}")], CancellationToken), CancellationToken));

        var results = await Task.WhenAll(saves);

        (results.All(result => result.Match(_ => true, _ => false)), (await Page(FirstAccount, "", Option.None<RelativeSyncPath>(), 100)).Count).ShouldBe((true, 1));
    }

    [Fact]
    public async Task when_the_baseline_is_counted_then_folders_and_files_of_that_account_only_are_counted()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FolderEntry(FirstAccount, "Docs", "f1"), FolderEntry(FirstAccount, "Docs/Sub", "f2"), FileEntry(FirstAccount, "Docs/a.txt", "1"), FileEntry(FirstAccount, "Docs/Sub/b.txt", "2"), FileEntry(FirstAccount, "c.txt", "3")], CancellationToken);
        _ = await Database.BaselineWriter.UpsertAsync(SecondAccount, [FolderEntry(SecondAccount, "Other", "f9"), FileEntry(SecondAccount, "Other/z.txt", "9")], CancellationToken);

        var counts = await Database.BaselineCounter.CountAsync(FirstAccount, CancellationToken);

        counts.Match(found => (found.Folders, found.Files), _ => (-1, -1)).ShouldBe((2, 3));
    }

    [Fact]
    public async Task when_an_account_has_no_baseline_then_the_counts_are_zero()
    {
        var counts = await Database.BaselineCounter.CountAsync(FirstAccount, CancellationToken);

        counts.Match(found => (found.Folders, found.Files), _ => (-1, -1)).ShouldBe((0, 0));
    }

    [Fact]
    public async Task when_an_entry_is_saved_again_with_a_casing_variant_then_it_is_counted_once()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "Docs/a.txt", "1")], CancellationToken);
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "DOCS/A.TXT", "1")], CancellationToken);

        var counts = await Database.BaselineCounter.CountAsync(FirstAccount, CancellationToken);

        counts.Match(found => (found.Folders, found.Files), _ => (-1, -1)).ShouldBe((0, 1));
    }

    [Fact]
    public async Task when_folders_and_files_are_saved_with_casing_variants_then_each_kind_is_counted_once_per_path()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FolderEntry(FirstAccount, "Docs", "f1"), FileEntry(FirstAccount, "Docs/a.txt", "1")], CancellationToken);
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FolderEntry(FirstAccount, "DOCS", "f1"), FileEntry(FirstAccount, "DOCS/A.TXT", "1"), FileEntry(FirstAccount, "docs/b.txt", "2")], CancellationToken);

        var counts = await Database.BaselineCounter.CountAsync(FirstAccount, CancellationToken);

        counts.Match(found => (found.Folders, found.Files), _ => (-1, -1)).ShouldBe((1, 2));
    }

    [Fact]
    public async Task when_the_baseline_is_large_then_the_counts_are_exact()
    {
        var folders = Enumerable.Range(0, 1_200).Select(index => FolderEntry(FirstAccount, $"Folder{index}", $"f{index}"));
        var files = Enumerable.Range(0, 3_300).Select(index => FileEntry(FirstAccount, $"Folder{index % 1_200}/file{index}.txt", $"i{index}"));
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [.. folders, .. files], CancellationToken);
        _ = await Database.BaselineWriter.UpsertAsync(SecondAccount, [FileEntry(SecondAccount, "x.txt", "x")], CancellationToken);

        var counts = await Database.BaselineCounter.CountAsync(FirstAccount, CancellationToken);

        counts.Match(found => (found.Folders, found.Files), _ => (-1, -1)).ShouldBe((1_200, 3_300));
    }

    [Fact]
    public async Task when_the_baseline_is_counted_while_it_is_saved_to_then_every_count_succeeds_and_the_final_count_is_exact()
    {
        var saves = Enumerable.Range(0, 20).Select(index => Task.Run(() => Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, $"Docs/{index}.txt", $"i{index}")], CancellationToken), CancellationToken));
        var counts = Enumerable.Range(0, 20).Select(_ => Task.Run(() => Database.BaselineCounter.CountAsync(FirstAccount, CancellationToken), CancellationToken));

        var counted = await Task.WhenAll(counts);
        _ = await Task.WhenAll(saves);

        counted.ShouldAllBe(result => result.Match(_ => true, _ => false));
        (await Database.BaselineCounter.CountAsync(FirstAccount, CancellationToken)).Match(found => found.Files, _ => -1).ShouldBe(20);
    }

    [Fact]
    public async Task when_the_baseline_is_counted_with_a_cancelled_token_then_the_count_is_cancelled()
    {
        var count = () => Database.BaselineCounter.CountAsync(FirstAccount, new CancellationToken(true));

        await Should.ThrowAsync<OperationCanceledException>(count);
    }

    [Fact]
    public async Task when_the_baseline_cannot_be_counted_then_the_count_fails_instead_of_being_zero()
    {
        await using var unmigrated = TemporarySyncDatabase.CreateUnmigrated();

        var counts = await unmigrated.BaselineCounter.CountAsync(FirstAccount, CancellationToken);

        counts.Match(_ => "counted", error => error.Message).ShouldBe("The baseline could not be counted.");
    }

    [Fact]
    public async Task when_a_stored_row_is_corrupt_then_the_page_read_fails_instead_of_returning_a_short_page()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "Docs/a.txt", "1"), FileEntry(FirstAccount, "Docs/c.txt", "3")], CancellationToken);
        await InsertCorruptFileRowAsync("Docs/b.txt");

        var read = await Database.BaselineReader.GetUnderAsync(FirstAccount, default, Option.None<RelativeSyncPath>(), 100, CancellationToken);

        read.Match(_ => "page", error => error.Message).ShouldBe("The baseline could not be read.");
    }

    [Fact]
    public async Task when_the_requested_row_is_corrupt_then_the_single_read_fails_instead_of_reporting_none()
    {
        await InsertCorruptFileRowAsync("Docs/b.txt");

        var read = await Database.BaselineReader.GetAsync(FirstAccount, Path("Docs/b.txt"), CancellationToken);

        read.Match(_ => "read", error => error.Message).ShouldBe("The baseline entry could not be read.");
    }

    [Theory]
    [InlineData("100%")]
    [InlineData("a_b")]
    [InlineData("back\\slash")]
    public async Task when_a_folder_name_holds_a_like_wildcard_then_only_that_folder_and_its_children_come_back(string folder)
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FolderEntry(FirstAccount, folder, "folder"), FileEntry(FirstAccount, $"{folder}/in.txt", "child"), FileEntry(FirstAccount, "100x/out.txt", "other-percent"), FileEntry(FirstAccount, "axb/out.txt", "other-underscore"), FileEntry(FirstAccount, "backXslash/out.txt", "other-backslash")], CancellationToken);

        var page = await Page(FirstAccount, folder, Option.None<RelativeSyncPath>(), 100);

        string.Join(',', page.Select(entry => entry.ItemId.Value).Order()).ShouldBe("child,folder");
    }

    [Fact]
    public async Task when_more_entries_than_one_chunk_are_saved_then_every_entry_is_stored_once()
    {
        var entries = Enumerable.Range(0, 1200).Select(index => FileEntry(FirstAccount, $"Docs/file{index:0000}.txt", $"item-{index:0000}")).ToList();
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, entries, CancellationToken);
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, entries.Select(entry => FileEntry(FirstAccount, entry.Path.Value!.ToUpperInvariant(), entry.ItemId.Value, hash: "second")).ToList(), CancellationToken);

        var page = await Page(FirstAccount, "", Option.None<RelativeSyncPath>(), 5000);

        (page.Count, page.All(entry => ((SyncBaselineKind.File)entry.Kind).Hash.Value == "second")).ShouldBe((1200, true));
    }

    [Fact]
    public async Task when_the_page_cursor_differs_only_in_case_then_paging_continues_after_that_path()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "a.txt", "1"), FileEntry(FirstAccount, "b.txt", "2"), FileEntry(FirstAccount, "c.txt", "3")], CancellationToken);

        var page = await Page(FirstAccount, "", Option.Some(Path("B.TXT")), 100);

        page.Select(entry => entry.ItemId.Value).ShouldBe(["3"]);
    }

    [Fact]
    public async Task when_a_huge_limit_is_asked_then_the_page_is_still_capped()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "a.txt", "1")], CancellationToken);

        var read = await Database.BaselineReader.GetUnderAsync(FirstAccount, default, Option.None<RelativeSyncPath>(), int.MaxValue, CancellationToken);

        read.Match(page => page.Count, _ => -1).ShouldBe(1);
    }

    [Fact]
    public async Task when_the_token_is_already_cancelled_then_a_save_throws_cancelled_and_changes_nothing()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "a.txt", "1")], source.Token));

        (await Page(FirstAccount, "", Option.None<RelativeSyncPath>(), 100)).Count.ShouldBe(0);
    }

    [Fact]
    public async Task when_no_entries_are_saved_then_the_save_succeeds_and_nothing_is_stored()
    {
        var saved = await Database.BaselineWriter.UpsertAsync(FirstAccount, [], CancellationToken);

        (saved.Match(_ => true, _ => false), (await Page(FirstAccount, "", Option.None<RelativeSyncPath>(), 100)).Count).ShouldBe((true, 0));
    }

    [Fact]
    public async Task when_the_module_is_added_then_reader_and_writer_share_one_store_and_the_store_takes_part_in_account_removal()
    {
        var participants = Database.Services.GetServices<IAccountRemovalParticipant>().Select(participant => participant.GetType().Name);

        (Database.BaselineReader.GetType(), Database.BaselineWriter.GetType()).ShouldBe((Database.BaselineWriter.GetType(), Database.BaselineWriter.GetType()));
        participants.ShouldContain("SqliteSyncBaselineStore");
    }

    private async Task InsertCorruptFileRowAsync(string path)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database.DatabasePath }.ToString());
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO SyncBaseline (AccountId, Path, PathKey, RemoteItemId, IsFolder) VALUES ($account, $path, upper($path), 'corrupt', 0)";
        _ = command.Parameters.AddWithValue("$account", FirstAccount.Value.ToString().ToUpperInvariant());
        _ = command.Parameters.AddWithValue("$path", path);
        _ = await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static RelativeSyncPath Path(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));

    private static SyncBaselineEntry FileEntry(AccountId account, string path, string itemId, long size = 10, string hash = "hash-1", string cTag = "c-1", string eTag = "e-1") =>
        SyncBaselineEntryFactory.Create(account, Path(path), Id(itemId), SyncBaselineKindFactory.CreateFile(size, WrittenAt, HashOf(hash).Match(value => value, () => throw new InvalidOperationException("The test hash must be valid.")), Tag(cTag), ETag(eTag)));

    private static SyncBaselineEntry FolderEntry(AccountId account, string path, string itemId) => SyncBaselineEntryFactory.Create(account, Path(path), Id(itemId), SyncBaselineKindFactory.CreateFolder());

    private async Task<IReadOnlyList<SyncBaselineEntry>> Page(AccountId account, string under, Option<RelativeSyncPath> after, int limit)
    {
        var read = await Database.BaselineReader.GetUnderAsync(account, under.Length == 0 ? default : Path(under), after, limit, CancellationToken);

        return read.Match(page => page, _ => throw new InvalidOperationException("The read must succeed."));
    }

    [Fact]
    public async Task when_an_entry_is_saved_again_in_another_non_ascii_casing_then_the_row_is_updated_not_duplicated()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "Docs/\u00c9t\u00e9.txt", "item-1", hash: "h1")], CancellationToken);
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "docs/\u00e9T\u00c9.TXT", "item-1", hash: "h2")], CancellationToken);

        var page = await Page(FirstAccount, "", Option.None<RelativeSyncPath>(), 100);

        page.Select(entry => ((SyncBaselineKind.File)entry.Kind).Hash.Value).ShouldBe(["h2"]);
    }

    [Fact]
    public async Task when_a_non_ascii_path_is_read_in_another_casing_then_the_entry_is_found_and_listed_under_its_folder()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FolderEntry(FirstAccount, "\u00c9coles", "folder-1"), FileEntry(FirstAccount, "\u00c9coles/\u00c9l\u00e8ve.txt", "item-1")], CancellationToken);

        var found = await Database.BaselineReader.GetAsync(FirstAccount, Path("\u00e9coles/\u00e9L\u00c8VE.txt"), CancellationToken);
        var under = await Page(FirstAccount, "\u00e9COLES", Option.None<RelativeSyncPath>(), 100);

        (found.Match(option => option.Match(_ => true, () => false), _ => false), under.Count).ShouldBe((true, 2));
    }

    [Fact]
    public async Task when_a_non_ascii_path_is_removed_in_another_casing_then_the_entry_is_gone()
    {
        _ = await Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "\u00c9t\u00e9.txt", "item-1")], CancellationToken);

        _ = await Database.BaselineWriter.RemoveAsync(FirstAccount, [Path("\u00e9T\u00c9.TXT")], CancellationToken);

        (await Page(FirstAccount, "", Option.None<RelativeSyncPath>(), 100)).ShouldBeEmpty();
    }
}
