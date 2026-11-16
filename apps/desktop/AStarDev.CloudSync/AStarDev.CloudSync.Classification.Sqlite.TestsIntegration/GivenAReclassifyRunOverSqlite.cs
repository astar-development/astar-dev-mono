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

    private ClassificationReclassifier Reclassifier() =>
        new(Database.Files, new FileClassifier([new ExtensionSignal(), new FolderSegmentSignal(), new DictionaryTermSignal()], new ClassificationVocabularyProvider(Database.Terms, Database.Categories)), Database.Links, ReclassifyOptionsFactory.Create(1, 1, TimeSpan.Zero), NullLogger<ClassificationReclassifier>.Instance);

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
}
