using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenASyncRootStore : IAsyncLifetime
{
    private const string OneDrive = "/home/jbarden/CloudSync/OneDrive";
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
    public async Task when_an_account_has_no_root_then_none_is_read()
    {
        var read = await Database.Reader.GetAsync(FirstAccount, CancellationToken);

        read.Match(root => root.Match(_ => "root", () => "none"), error => error.Message).ShouldBe("none");
    }

    [Fact]
    public async Task when_a_root_is_set_then_it_is_saved_and_read_back_normalised()
    {
        var set = await Database.Writer.SetAsync(FirstAccount, Database.Folder(OneDrive + "/"), CancellationToken);

        set.Match(root => root.Directory.FullName, _ => string.Empty).ShouldBe(OneDrive);
        (await Database.Reader.GetAsync(FirstAccount, CancellationToken)).Match(root => root.Match(found => found.Directory.FullName, () => "none"), error => error.Message).ShouldBe(OneDrive);
    }

    [Fact]
    public async Task when_the_root_is_changed_then_the_new_root_replaces_the_old_one()
    {
        _ = await Database.Writer.SetAsync(FirstAccount, Database.Folder(OneDrive), CancellationToken);

        _ = await Database.Writer.SetAsync(FirstAccount, Database.Folder(OneDrive + "/Work"), CancellationToken);

        (await Database.Reader.GetAllAsync(CancellationToken)).Match(roots => roots.Select(root => root.Root.Directory.FullName), _ => []).ShouldBe([OneDrive + "/Work"]);
    }

    [Fact]
    public async Task when_a_root_would_cross_another_accounts_root_then_it_is_rejected_naming_that_account_and_nothing_changes()
    {
        _ = await Database.Writer.SetAsync(FirstAccount, Database.Folder(OneDrive), CancellationToken);
        _ = await Database.Writer.SetAsync(SecondAccount, Database.Folder("/home/jbarden/CloudSync/Other"), CancellationToken);

        var set = await Database.Writer.SetAsync(SecondAccount, Database.Folder(OneDrive + "/Work"), CancellationToken);

        set.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateInsideOf(FirstAccount));
        (await Database.Reader.GetAsync(SecondAccount, CancellationToken)).Match(root => root.Match(found => found.Directory.FullName, () => "none"), error => error.Message).ShouldBe("/home/jbarden/CloudSync/Other");
    }

    [Theory]
    [InlineData("/home/jbarden")]
    [InlineData("/home/jbarden/")]
    [InlineData("/HOME/JBARDEN")]
    [InlineData("/")]
    public async Task when_the_root_is_the_home_folder_or_a_filesystem_root_then_it_is_rejected_as_reserved(string path)
    {
        var set = await Database.Writer.SetAsync(FirstAccount, Database.Folder(path), CancellationToken);

        set.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateReserved());
        (await Database.Reader.GetAllAsync(CancellationToken)).Match(roots => roots.Count, _ => -1).ShouldBe(0);
    }

    [Fact]
    public async Task when_the_same_folder_is_set_for_twenty_accounts_at_once_then_exactly_one_wins_and_the_rest_are_rejected_as_the_same()
    {
        var accounts = Enumerable.Range(0, 20).Select(_ => AccountIdFactory.Create(Guid.NewGuid())).ToList();
        var start = new TaskCompletionSource();
        var pending = accounts.Select(account => Task.Run(async () => await start.Task.ContinueWith(_ => Database.Writer.SetAsync(account, Database.Folder(OneDrive), CancellationToken), CancellationToken).Unwrap(), CancellationToken)).ToList();

        start.SetResult();
        var results = await Task.WhenAll(pending);

        results.Count(result => result.Match(_ => true, _ => false)).ShouldBe(1);
        results.Select(result => result.Match(_ => string.Empty, rejection => rejection.GetType().Name)).Where(name => name.Length > 0).ShouldAllBe(name => name == nameof(SyncRootRejection.SameAs));
    }

    [Fact]
    public async Task when_a_folder_and_its_sub_folders_are_set_for_different_accounts_at_once_then_no_two_stored_roots_cross()
    {
        var folders = new[] { OneDrive, OneDrive + "/A", OneDrive + "/A/B", OneDrive + "/C", OneDrive + "/A", OneDrive };
        var accounts = folders.Select(_ => AccountIdFactory.Create(Guid.NewGuid())).ToList();

        var results = await Task.WhenAll(folders.Select((folder, index) => Task.Run(() => Database.Writer.SetAsync(accounts[index], Database.Folder(folder), CancellationToken), CancellationToken)));

        results.Select(result => result.Match(_ => string.Empty, rejection => rejection.GetType().Name)).ShouldNotContain(nameof(SyncRootRejection.Store));
        var stored = (await Database.Reader.GetAllAsync(CancellationToken)).Match(roots => roots.Select(root => root.Root.Directory.FullName).ToList(), _ => []);
        stored.ShouldAllBe(root => stored.Count(other => other.Equals(root, StringComparison.OrdinalIgnoreCase) || other.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase) || root.StartsWith(other + "/", StringComparison.OrdinalIgnoreCase)) == 1);
    }

    [Fact]
    public async Task when_an_account_data_is_removed_then_its_root_is_gone_and_the_others_stay()
    {
        _ = await Database.Writer.SetAsync(FirstAccount, Database.Folder(OneDrive), CancellationToken);
        _ = await Database.Writer.SetAsync(SecondAccount, Database.Folder("/home/jbarden/CloudSync/Other"), CancellationToken);

        var removed = await Database.Participant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        removed.Match(_ => true, _ => false).ShouldBeTrue();
        (await Database.Reader.GetAllAsync(CancellationToken)).Match(roots => roots.Select(root => root.AccountId), _ => []).ShouldBe([SecondAccount]);
    }

    [Fact]
    public async Task when_data_is_removed_for_an_account_without_a_root_then_it_succeeds()
    {
        var removed = await Database.Participant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        removed.Match(_ => true, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_database_is_migrated_again_then_it_succeeds_and_keeps_the_roots()
    {
        _ = await Database.Writer.SetAsync(FirstAccount, Database.Folder(OneDrive), CancellationToken);

        var migrated = await Database.Database.MigrateAsync(CancellationToken);

        migrated.Match(_ => true, _ => false).ShouldBeTrue();
        (await Database.Reader.GetAllAsync(CancellationToken)).Match(roots => roots.Count, _ => -1).ShouldBe(1);
    }

    [Fact]
    public async Task when_the_database_is_migrated_then_the_accounts_still_work()
    {
        var registration = AccountRegistrationFactory.Create(ProviderAccountKeyFactory.Create("key-1").Match(key => key, _ => throw new InvalidOperationException()), CloudProviderFactory.CreateOneDrive(), "Personal", "personal@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1, 2));

        var outcome = await Database.AccountStore.AddAsync(registration, CancellationToken);

        outcome.ShouldBeOfType<AccountAddOutcome.Added>();
    }

    [Fact]
    public async Task when_the_root_changes_then_only_that_accounts_baseline_is_cleared()
    {
        _ = await Database.Writer.SetAsync(FirstAccount, Database.Folder(OneDrive), CancellationToken);
        _ = await Database.Writer.SetAsync(SecondAccount, Database.Folder("/home/jbarden/CloudSync/Other"), CancellationToken);
        await SeedBaselineAsync(FirstAccount, 3);
        await SeedBaselineAsync(SecondAccount, 2);

        _ = await Database.Writer.SetAsync(FirstAccount, Database.Folder("/home/jbarden/CloudSync/Elsewhere"), CancellationToken);

        (await BaselineCountAsync(FirstAccount), await BaselineCountAsync(SecondAccount)).ShouldBe((0, 2));
    }

    [Fact]
    public async Task when_the_same_root_is_saved_again_then_the_baseline_is_kept()
    {
        _ = await Database.Writer.SetAsync(FirstAccount, Database.Folder(OneDrive), CancellationToken);
        await SeedBaselineAsync(FirstAccount, 3);

        _ = await Database.Writer.SetAsync(FirstAccount, Database.Folder(OneDrive + "/"), CancellationToken);

        (await BaselineCountAsync(FirstAccount)).ShouldBe(3);
    }

    [Fact]
    public async Task when_a_root_change_is_rejected_then_the_baseline_is_kept()
    {
        _ = await Database.Writer.SetAsync(FirstAccount, Database.Folder(OneDrive), CancellationToken);
        _ = await Database.Writer.SetAsync(SecondAccount, Database.Folder("/home/jbarden/CloudSync/Other"), CancellationToken);
        await SeedBaselineAsync(SecondAccount, 3);

        var set = await Database.Writer.SetAsync(SecondAccount, Database.Folder(OneDrive + "/Work"), CancellationToken);

        (set.Match(_ => "saved", _ => "rejected"), await BaselineCountAsync(SecondAccount)).ShouldBe(("rejected", 3));
    }

    [Fact]
    public async Task when_a_root_is_set_for_the_first_time_then_an_existing_baseline_is_kept()
    {
        await SeedBaselineAsync(FirstAccount, 3);

        _ = await Database.Writer.SetAsync(FirstAccount, Database.Folder(OneDrive), CancellationToken);

        (await BaselineCountAsync(FirstAccount)).ShouldBe(3);
    }

    private async Task SeedBaselineAsync(AccountId accountId, int count)
    {
        var entries = Enumerable.Range(0, count).Select(index => SyncBaselineEntryFactory.Create(accountId, RelativePath($"Docs{index}"), RemoteItemIdFactory.Create($"item-{index}").Match(id => id, _ => throw new InvalidOperationException("The test id must be valid.")), SyncBaselineKindFactory.CreateFolder())).ToList();
        _ = await Database.BaselineWriter.UpsertAsync(accountId, entries, CancellationToken);
    }

    private async Task<int> BaselineCountAsync(AccountId accountId) =>
        (await Database.BaselineReader.GetUnderAsync(accountId, default, Option.None<RelativeSyncPath>(), 100, CancellationToken)).Match(page => page.Count, _ => -1);

    private static RelativeSyncPath RelativePath(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));
}
