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

        page.Select(entry => entry.ItemId.Value).ShouldBe(["p", "pa", "pb"]);
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

        (pages, seen).ShouldBe((3, entries.Select(entry => entry.ItemId.Value).ToList()));
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
    public async Task when_the_token_is_already_cancelled_then_a_save_changes_nothing()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var outcome = await Record.ExceptionAsync(() => Database.BaselineWriter.UpsertAsync(FirstAccount, [FileEntry(FirstAccount, "a.txt", "1")], source.Token));

        (outcome is null || outcome is OperationCanceledException, (await Page(FirstAccount, "", Option.None<RelativeSyncPath>(), 100)).Count).ShouldBe((true, 0));
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

    private static RelativeSyncPath Path(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));

    private static SyncBaselineEntry FileEntry(AccountId account, string path, string itemId, long size = 10, string hash = "hash-1", string cTag = "c-1", string eTag = "e-1") =>
        SyncBaselineEntryFactory.Create(account, Path(path), Id(itemId), SyncBaselineKindFactory.CreateFile(size, WrittenAt, HashOf(hash).Match(value => value, () => throw new InvalidOperationException("The test hash must be valid.")), Tag(cTag), ETag(eTag)));

    private static SyncBaselineEntry FolderEntry(AccountId account, string path, string itemId) => SyncBaselineEntryFactory.Create(account, Path(path), Id(itemId), SyncBaselineKindFactory.CreateFolder());

    private async Task<IReadOnlyList<SyncBaselineEntry>> Page(AccountId account, string under, Option<RelativeSyncPath> after, int limit)
    {
        var read = await Database.BaselineReader.GetUnderAsync(account, under.Length == 0 ? default : Path(under), after, limit, CancellationToken);

        return read.Match(page => page, _ => throw new InvalidOperationException("The read must succeed."));
    }
}
