using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAClassifyingUploadRecorder
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.NewGuid());

    private static SyncBaselineEntry FileEntry(string path) => SyncBaselineEntryFactory.Create(Account, PathOf(path), Id("item-" + path), SyncBaselineKindFactory.CreateFile(10, DateTimeOffset.UnixEpoch, Hash(), Tag(), ETag()));

    private static SyncBaselineEntry FolderEntry(string path) => SyncBaselineEntryFactory.Create(Account, PathOf(path), Id("item-" + path), SyncBaselineKindFactory.CreateFolder());

    private static RemoteItemSnapshot Remote(string path) =>
        RemoteItemSnapshotFactory.Create(Id("item-" + path), Option.None<RemoteItemId>(), "name", RemoteItemKindFactory.CreateFile(10, Option.None<ContentHash>(), Tag()), DateTimeOffset.UnixEpoch, ETag());

    private static RemoteItemId Id(string value) => RemoteItemIdFactory.Create(value).Match(id => id, _ => throw new InvalidOperationException());

    private static ContentHash Hash() => ContentHashFactory.Create("h").Match(hash => hash, _ => throw new InvalidOperationException());

    private static ContentTag Tag() => ContentTagFactory.Create("c").Match(tag => tag, _ => throw new InvalidOperationException());

    private static EntityTag ETag() => EntityTagFactory.Create("e").Match(tag => tag, _ => throw new InvalidOperationException());

    private static ClassifyingUploadRecorder Build(UploadHookInnerRecorder inner, ClassifyHookClassifier classifier, ClassifyHookStore store, ILogger<ClassifyingUploadRecorder>? logger = null, IClassificationVocabularyProvider? vocabulary = null) =>
        new(inner, classifier, store, vocabulary ?? new RecordingVocabularyProvider(new Ok<ClassificationVocabulary, ClassificationFailure>(ClassificationVocabularyFactory.Create([], []))), logger ?? NullLogger<ClassifyingUploadRecorder>.Instance);

    private static Task<Result<Unit, ErrorResponse>> Record(ClassifyingUploadRecorder recorder, SyncBaselineEntry entry, CancellationToken cancellationToken) =>
        recorder.RecordAsync(Account, entry, Remote(entry.Path.Value ?? string.Empty), cancellationToken);

    private static bool Succeeded(Result<Unit, ErrorResponse> result) => result.Match(_ => true, _ => false);

    [Fact]
    public async Task when_an_upload_is_recorded_then_the_file_is_classified_and_its_links_replaced()
    {
        var inner = new UploadHookInnerRecorder();
        var store = new ClassifyHookStore();
        var recorder = Build(inner, new ClassifyHookClassifier(), store);

        var result = await Record(recorder, FileEntry("Holiday/a.jpg"), TestContext.Current.CancellationToken);

        Succeeded(result).ShouldBeTrue();
        inner.Calls.ShouldBe(1);
        store.Replaced.Select(links => links.Path.Value).ShouldBe(["Holiday/a.jpg"]);
        store.Replaced[0].AccountId.ShouldBe(Account);
        store.Replaced[0].TermIds.ShouldBe([ClassifyHookClassifier.TermId]);
    }

    [Fact]
    public async Task when_the_vocabulary_changes_while_the_file_is_classified_then_it_is_classified_and_written_again()
    {
        var vocabulary = new RecordingVocabularyProvider(new Ok<ClassificationVocabulary, ClassificationFailure>(ClassificationVocabularyFactory.Create([], [])));
        var classifier = new ClassifyHookClassifier { OnClassify = () => { if (vocabulary.Invalidations == 0) vocabulary.Invalidate(); } };
        var store = new ClassifyHookStore();
        var recorder = Build(new UploadHookInnerRecorder(), classifier, store, vocabulary: vocabulary);

        _ = await Record(recorder, FileEntry("Holiday/a.jpg"), TestContext.Current.CancellationToken);

        classifier.Paths.Count.ShouldBe(2);
        store.Replaced.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_vocabulary_keeps_changing_then_the_file_is_not_reclassified_forever()
    {
        var vocabulary = new RecordingVocabularyProvider(new Ok<ClassificationVocabulary, ClassificationFailure>(ClassificationVocabularyFactory.Create([], [])));
        var classifier = new ClassifyHookClassifier { OnClassify = vocabulary.Invalidate };
        var recorder = Build(new UploadHookInnerRecorder(), classifier, new ClassifyHookStore(), vocabulary: vocabulary);

        var result = await Record(recorder, FileEntry("Holiday/a.jpg"), TestContext.Current.CancellationToken);

        Succeeded(result).ShouldBeTrue();
        classifier.Paths.Count.ShouldBeLessThan(10);
    }

    [Fact]
    public async Task when_a_folder_is_recorded_then_it_is_not_classified()
    {
        var classifier = new ClassifyHookClassifier();
        var store = new ClassifyHookStore();
        var recorder = Build(new UploadHookInnerRecorder(), classifier, store);

        var result = await Record(recorder, FolderEntry("Holiday"), TestContext.Current.CancellationToken);

        Succeeded(result).ShouldBeTrue();
        classifier.Paths.ShouldBeEmpty();
        store.Replaced.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_inner_record_fails_then_the_failure_is_returned_and_nothing_is_classified()
    {
        var inner = new UploadHookInnerRecorder { Outcome = Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")) };
        var classifier = new ClassifyHookClassifier();
        var store = new ClassifyHookStore();
        var recorder = Build(inner, classifier, store);

        var result = await Record(recorder, FileEntry("a.jpg"), TestContext.Current.CancellationToken);

        result.ShouldBe(inner.Outcome);
        Succeeded(result).ShouldBeFalse();
        classifier.Paths.ShouldBeEmpty();
        store.Replaced.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_classifier_throws_then_the_result_is_unchanged_and_a_warning_is_logged_without_the_message()
    {
        var inner = new UploadHookInnerRecorder();
        var logger = new RecordingLogger<ClassifyingUploadRecorder>();
        var classifier = new ClassifyHookClassifier { Throws = () => new InvalidOperationException("secret holiday.jpg term") };
        var recorder = Build(inner, classifier, new ClassifyHookStore(), logger);

        var result = await Record(recorder, FileEntry("secret/holiday.jpg"), TestContext.Current.CancellationToken);

        result.ShouldBe(inner.Outcome);
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        logger.Messages.ShouldAllBe(message => !message.Contains("secret") && !message.Contains("holiday"));
        logger.Messages.ShouldContain(message => message.Contains(nameof(InvalidOperationException)));
    }

    [Fact]
    public async Task when_the_store_throws_then_the_result_is_unchanged_and_a_warning_is_logged()
    {
        var inner = new UploadHookInnerRecorder();
        var logger = new RecordingLogger<ClassifyingUploadRecorder>();
        var store = new ClassifyHookStore { Throws = () => new TimeoutException("db secret") };
        var recorder = Build(inner, new ClassifyHookClassifier(), store, logger);

        var result = await Record(recorder, FileEntry("a.jpg"), TestContext.Current.CancellationToken);

        result.ShouldBe(inner.Outcome);
        logger.Entries.Count(entry => entry.Level == LogLevel.Warning).ShouldBe(1);
        logger.Messages.ShouldAllBe(message => !message.Contains("secret"));
    }

    [Fact]
    public async Task when_the_store_returns_a_failure_then_the_result_is_unchanged_and_a_warning_is_logged()
    {
        var inner = new UploadHookInnerRecorder();
        var logger = new RecordingLogger<ClassifyingUploadRecorder>();
        var recorder = Build(inner, new ClassifyHookClassifier(), new ClassifyHookStore { Fails = true }, logger);

        var result = await Record(recorder, FileEntry("a.jpg"), TestContext.Current.CancellationToken);

        result.ShouldBe(inner.Outcome);
        logger.Entries.Count(entry => entry.Level == LogLevel.Warning).ShouldBe(1);
    }

    [Fact]
    public async Task when_the_upload_is_cancelled_during_classification_then_the_cancellation_passes_through()
    {
        using var source = new CancellationTokenSource();
        var classifier = new ClassifyHookClassifier { Throws = () => { source.Cancel(); return new OperationCanceledException(source.Token); } };
        var recorder = Build(new UploadHookInnerRecorder(), classifier, new ClassifyHookStore());

        var act = () => Record(recorder, FileEntry("a.jpg"), source.Token);

        _ = await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task when_paths_differ_only_by_case_then_each_is_classified_under_its_own_spelling()
    {
        var classifier = new ClassifyHookClassifier();
        var store = new ClassifyHookStore();
        var recorder = Build(new UploadHookInnerRecorder(), classifier, store);

        _ = await Record(recorder, FileEntry("Holiday/A.JPG"), TestContext.Current.CancellationToken);
        _ = await Record(recorder, FileEntry("holiday/a.jpg"), TestContext.Current.CancellationToken);

        classifier.Paths.ShouldBe(["Holiday/A.JPG", "holiday/a.jpg"]);
        store.Replaced.Select(links => links.Path.Value).ShouldBe(["Holiday/A.JPG", "holiday/a.jpg"]);
    }

    [Fact]
    public async Task when_twenty_uploads_are_recorded_in_parallel_then_every_file_is_classified_and_every_result_is_the_inner_result()
    {
        var inner = new UploadHookInnerRecorder();
        var store = new ClassifyHookStore();
        var recorder = Build(inner, new ClassifyHookClassifier(), store);

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(index => Task.Run(() => Record(recorder, FileEntry($"d{index}/f.jpg"), TestContext.Current.CancellationToken), TestContext.Current.CancellationToken)));

        results.ShouldAllBe(result => result.Equals(inner.Outcome));
        inner.Calls.ShouldBe(20);
        store.Replaced.Select(links => links.Path.Value).Order().ShouldBe(Enumerable.Range(0, 20).Select(index => $"d{index}/f.jpg").Order());
    }

    [Fact]
    public async Task when_the_classifier_throws_then_the_warning_carries_the_error_type_as_its_own_value_and_a_fixed_message()
    {
        var logger = new RecordingLogger<ClassifyingUploadRecorder>();
        var classifier = new ClassifyHookClassifier { Throws = () => new InvalidOperationException("secret holiday.jpg term") };
        var recorder = Build(new UploadHookInnerRecorder(), classifier, new ClassifyHookStore(), logger);

        await Record(recorder, FileEntry("secret/holiday.jpg"), TestContext.Current.CancellationToken);

        var values = logger.Events.ShouldHaveSingleItem().Values;
        (values["ErrorType"], values["Message"]).ShouldBe(("System.InvalidOperationException", "An uploaded file could not be classified after the baseline write"));
    }

    [Fact]
    public async Task when_the_store_returns_a_failure_then_the_warning_carries_the_failure_type_as_its_own_value_and_a_fixed_message()
    {
        var logger = new RecordingLogger<ClassifyingUploadRecorder>();
        var recorder = Build(new UploadHookInnerRecorder(), new ClassifyHookClassifier(), new ClassifyHookStore { Fails = true }, logger);

        await Record(recorder, FileEntry("a.jpg"), TestContext.Current.CancellationToken);

        var values = logger.Events.ShouldHaveSingleItem().Values;
        values["Message"].ShouldBe("The classification links were not updated after the upload was recorded");
        values["ErrorType"].ShouldBeOfType<string>().ShouldNotBeEmpty();
    }
}
