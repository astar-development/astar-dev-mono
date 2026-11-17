using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;
using static AStarDev.CloudSync.Classification.Sqlite.TestsIntegration.ClassificationData;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenAReclassifyRunOverSqlite : MigratedDatabaseTests
{
    private static readonly DateTimeOffset WrittenAt = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    private async Task<AccountId> AddAccountAsync()
    {
        var providerKey = ProviderAccountKeyFactory.Create("one").Match(created => created, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(providerKey, CloudProviderFactory.CreateOneDrive(), "one", "one@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1, 2));

        return (await Database.AccountStore.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }

    private static T Valid<T>(Result<T, ErrorResponse> result) => result.Match(value => value, _ => throw new InvalidOperationException("The test value must be valid."));

    private static SyncBaselineEntry FileEntry(AccountId accountId, string path, string itemId) =>
        SyncBaselineEntryFactory.Create(accountId, Path(path), Valid(RemoteItemIdFactory.Create(itemId)), SyncBaselineKindFactory.CreateFile(10, WrittenAt, Valid(ContentHashFactory.Create("h")), Valid(ContentTagFactory.Create("c")), Valid(EntityTagFactory.Create("e"))));

    private async Task<TermId> AddTermAsync(string text, CategoryId category, TermStatus? status = null) => Ok(await Database.Terms.UpsertAsync(Term(text, category, status), CancellationToken)).Id;

    private ClassificationReclassifier Reclassifier(IAccountSyncLock? syncLock = null) =>
        new(Database.Files, new FileClassifier([new ExtensionSignal(), new FolderSegmentSignal(), new DictionaryTermSignal()], new ClassificationVocabularyProvider(Database.Terms, Database.Categories)), Database.Links, syncLock ?? AccountSyncLocks.Create(), Database.AccountStore, ReclassifyOptionsFactory.Create(1, 1, TimeSpan.Zero), NullLogger<ClassificationReclassifier>.Instance);

    private async Task RemoveAccountUnderLeaseAsync(IAccountSyncLock syncLock, AccountId account)
    {
        using var lease = syncLock.TryAcquire(account).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        _ = Valid(await Database.LinksParticipant.RemoveAccountDataAsync(account, CancellationToken));
        _ = Valid(await Database.AccountStore.RemoveAsync(account, CancellationToken));
    }

    private async Task<List<TermId>> TermsOfAsync(AccountId account, string path) => [.. Ok(await Database.Links.GetTermsForFileAsync(account, Path(path), CancellationToken)).OrderBy(term => term.Value)];

    [Fact]
    public async Task when_the_baseline_is_reclassified_then_user_links_and_ignored_terms_are_preserved_and_stale_auto_links_are_replaced()
    {
        var account = await AddAccountAsync();
        _ = Valid(await Database.BaselineWriter.UpsertAsync(account, [FileEntry(account, "Holiday/red car.jpg", "i1"), FileEntry(account, "Docs/notes.txt", "i2")], CancellationToken));
        var category = Ok(await Database.Categories.UpsertAsync(Category("Things"), CancellationToken)).Id;
        var jpg = await AddTermAsync("jpg", category);
        var holiday = await AddTermAsync("holiday", category);
        var red = await AddTermAsync("red", category);
        var car = await AddTermAsync("car", category, TermStatusFactory.CreateIgnored());
        var favourite = await AddTermAsync("favourite", category);
        var stale = await AddTermAsync("stale", category);
        _ = Ok(await Database.Links.AssignUserLinkAsync(account, Path("Holiday/red car.jpg"), favourite, CancellationToken));
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("Holiday/red car.jpg"), [stale], CancellationToken));
        _ = Ok(await Database.Links.ReplaceAutoLinksAsync(account, Path("Docs/notes.txt"), [stale], CancellationToken));

        var outcome = await Reclassifier().StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        outcome.ShouldBeOfType<ReclassifyOutcome.Completed>().Counts.FilesWritten.ShouldBe(2);
        (await TermsOfAsync(account, "Holiday/red car.jpg")).ShouldBe([.. new[] { jpg, holiday, red, favourite }.OrderBy(term => term.Value)]);
        (await TermsOfAsync(account, "Docs/notes.txt")).ShouldBeEmpty();
        Ok(await Database.Terms.GetAllAsync(CancellationToken)).Single(term => term.Id == car).Status.ShouldBeOfType<TermStatus.Ignored>();
    }

    [Fact]
    public async Task when_the_run_is_repeated_then_the_links_are_unchanged()
    {
        var account = await AddAccountAsync();
        _ = Valid(await Database.BaselineWriter.UpsertAsync(account, [FileEntry(account, "Holiday/red car.jpg", "i1")], CancellationToken));
        var category = Ok(await Database.Categories.UpsertAsync(Category("Things"), CancellationToken)).Id;
        _ = await AddTermAsync("jpg", category);
        _ = await AddTermAsync("red", category);
        _ = await Reclassifier().StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);
        var first = await TermsOfAsync(account, "Holiday/red car.jpg");

        var second = await Reclassifier().StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        second.ShouldBeOfType<ReclassifyOutcome.Completed>();
        (await TermsOfAsync(account, "Holiday/red car.jpg")).ShouldBe(first);
        first.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_account_is_removed_while_its_files_are_being_reclassified_then_no_links_are_left_for_it()
    {
        var account = await AddAccountAsync();
        _ = Valid(await Database.BaselineWriter.UpsertAsync(account, [FileEntry(account, "Holiday/a.jpg", "i1"), FileEntry(account, "Holiday/b.jpg", "i2")], CancellationToken));
        var category = Ok(await Database.Categories.UpsertAsync(Category("Things"), CancellationToken)).Id;
        _ = await AddTermAsync("jpg", category);
        var syncLock = AccountSyncLocks.Create();
        var progress = new RemovingAfterFirstBatchProgress(() => RemoveAccountUnderLeaseAsync(syncLock, account));

        var outcome = await Reclassifier(syncLock).StartAsync(progress, CancellationToken);

        var counts = outcome.ShouldBeOfType<ReclassifyOutcome.Completed>().Counts;
        (counts.FilesWritten, counts.SkippedForAccount).ShouldBe((1, 1));
        (await TermsOfAsync(account, "Holiday/a.jpg")).ShouldBeEmpty();
        (await TermsOfAsync(account, "Holiday/b.jpg")).ShouldBeEmpty();
    }
}
