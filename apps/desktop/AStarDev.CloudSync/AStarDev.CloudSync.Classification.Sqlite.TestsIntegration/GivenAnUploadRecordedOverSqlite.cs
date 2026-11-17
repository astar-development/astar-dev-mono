using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;
using static AStarDev.CloudSync.Classification.Sqlite.TestsIntegration.ClassificationData;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenAnUploadRecordedOverSqlite : MigratedDatabaseTests
{
    private static readonly DateTimeOffset WrittenAt = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    private async Task<AccountId> AddAccountAsync()
    {
        var providerKey = ProviderAccountKeyFactory.Create("one").Match(created => created, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(providerKey, CloudProviderFactory.CreateOneDrive(), "one", "one@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1, 2));

        return (await Database.AccountStore.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }

    private static T Valid<T>(Result<T, ErrorResponse> result) => result.Match(value => value, _ => throw new InvalidOperationException("The test value must be valid."));

    private static RemoteItemId Id(string value) => Valid(RemoteItemIdFactory.Create(value));

    private static SyncBaselineEntry FileEntry(AccountId accountId, string path) =>
        SyncBaselineEntryFactory.Create(accountId, Path(path), Id("item-1"), SyncBaselineKindFactory.CreateFile(10, WrittenAt, Valid(ContentHashFactory.Create("h")), Valid(ContentTagFactory.Create("c")), Valid(EntityTagFactory.Create("e"))));

    private static RemoteItemSnapshot Remote() =>
        RemoteItemSnapshotFactory.Create(Id("item-1"), Option.None<RemoteItemId>(), "red car.jpg", RemoteItemKindFactory.CreateFile(10, Option.None<ContentHash>(), Valid(ContentTagFactory.Create("c"))), WrittenAt, Valid(EntityTagFactory.Create("e")));

    [Fact]
    public async Task when_an_upload_is_recorded_through_the_classifying_recorder_then_the_baseline_row_and_the_auto_links_exist()
    {
        var account = await AddAccountAsync();
        var category = Ok(await Database.Categories.UpsertAsync(Category("Things"), CancellationToken)).Id;
        var jpg = Ok(await Database.Terms.UpsertAsync(Term("jpg", category), CancellationToken)).Id;
        var holiday = Ok(await Database.Terms.UpsertAsync(Term("holiday", category), CancellationToken)).Id;
        var vocabulary = new ClassificationVocabularyProvider(Database.Terms, Database.Categories);
        var classifier = new FileClassifier([new ExtensionSignal(), new FolderSegmentSignal(), new DictionaryTermSignal()], vocabulary);
        var recorder = new ClassifyingUploadRecorder(Database.UploadRecorder, classifier, Database.Links, vocabulary, NullLogger<ClassifyingUploadRecorder>.Instance);

        var result = await recorder.RecordAsync(account, FileEntry(account, "Holiday/red car.jpg"), Remote(), CancellationToken);

        Valid(result).ShouldBe(Unit.Instance);
        Valid(await Database.BaselineReader.GetAsync(account, Path("Holiday/red car.jpg"), CancellationToken)).Match(_ => true, () => false).ShouldBeTrue();
        Ok(await Database.Links.GetTermsForFileAsync(account, Path("Holiday/red car.jpg"), CancellationToken)).OrderBy(term => term.Value).ShouldBe([.. new[] { jpg, holiday }.OrderBy(term => term.Value)]);
    }
}
