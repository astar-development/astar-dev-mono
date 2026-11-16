using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenAClassifiableFileSource : MigratedDatabaseTests
{
    private static readonly DateTimeOffset WrittenAt = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    private async Task<AccountId> AddAccountAsync(string key)
    {
        var providerKey = ProviderAccountKeyFactory.Create(key).Match(created => created, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(providerKey, CloudProviderFactory.CreateOneDrive(), key, $"{key}@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1, 2));

        return (await Database.AccountStore.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }

    private static T Valid<T>(Result<T, ErrorResponse> result) => result.Match(value => value, _ => throw new InvalidOperationException("The test value must be valid."));

    private static SyncBaselineEntry FileEntry(AccountId accountId, string path, string itemId) =>
        SyncBaselineEntryFactory.Create(accountId, ClassificationData.Path(path), Valid(RemoteItemIdFactory.Create(itemId)), SyncBaselineKindFactory.CreateFile(10, WrittenAt, Valid(ContentHashFactory.Create("h")), Valid(ContentTagFactory.Create("c")), Valid(EntityTagFactory.Create("e"))));

    private static SyncBaselineEntry FolderEntry(AccountId accountId, string path, string itemId) => SyncBaselineEntryFactory.Create(accountId, ClassificationData.Path(path), Valid(RemoteItemIdFactory.Create(itemId)), SyncBaselineKindFactory.CreateFolder());

    private async Task SaveAsync(AccountId accountId, params SyncBaselineEntry[] entries) => Valid(await Database.BaselineWriter.UpsertAsync(accountId, entries, CancellationToken)).ShouldBe(Unit.Instance);

    private async Task<List<(AccountId, string?)>> StreamAsync()
    {
        var files = new List<(AccountId, string?)>();
        await foreach (var file in Database.Files.StreamAsync(CancellationToken))
            files.Add((file.AccountId, file.Path.Value));

        return files;
    }

    [Fact]
    public async Task when_no_account_exists_then_the_stream_is_empty()
    {
        (await StreamAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_baseline_has_files_and_folders_then_only_the_files_are_streamed()
    {
        var account = await AddAccountAsync("one");
        await SaveAsync(account, FolderEntry(account, "Docs", "f1"), FileEntry(account, "Docs/a.txt", "i1"), FileEntry(account, "b.txt", "i2"));

        var files = await StreamAsync();

        files.ShouldBe([(account, "Docs/a.txt"), (account, "b.txt")], ignoreOrder: true);
    }

    [Fact]
    public async Task when_there_are_two_accounts_then_the_files_of_both_are_streamed()
    {
        var first = await AddAccountAsync("one");
        var second = await AddAccountAsync("two");
        await SaveAsync(first, FileEntry(first, "a.txt", "i1"));
        await SaveAsync(second, FileEntry(second, "a.txt", "i2"));

        var files = await StreamAsync();

        files.ShouldBe([(first, "a.txt"), (second, "a.txt")], ignoreOrder: true);
    }

    [Fact]
    public async Task when_the_baseline_has_more_files_than_one_page_then_every_file_is_streamed_once()
    {
        var account = await AddAccountAsync("one");
        for (var chunk = 0; chunk < 13; chunk++)
            await SaveAsync(account, [.. Enumerable.Range(chunk * 500, 500).Select(index => FileEntry(account, $"Folder/file-{index:D5}.txt", $"item-{index}"))]);

        var files = await StreamAsync();

        (files.Count, files.Select(file => file.Item2).Distinct(StringComparer.OrdinalIgnoreCase).Count()).ShouldBe((6500, 6500));
    }

    [Fact]
    public async Task when_the_consumer_cancels_then_the_stream_stops()
    {
        var account = await AddAccountAsync("one");
        await SaveAsync(account, FileEntry(account, "a.txt", "i1"));
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var read = async () =>
        {
            await foreach (var file in Database.Files.StreamAsync(source.Token))
                _ = file;
        };

        await Should.ThrowAsync<OperationCanceledException>(read);
    }
}
