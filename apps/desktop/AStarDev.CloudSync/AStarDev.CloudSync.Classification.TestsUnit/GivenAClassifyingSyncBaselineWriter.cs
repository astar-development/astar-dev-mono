using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAClassifyingSyncBaselineWriter
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.NewGuid());

    private static SyncBaselineEntry FileEntry(string path) => SyncBaselineEntryFactory.Create(Account, PathOf(path), Id("item-" + path), SyncBaselineKindFactory.CreateFile(10, DateTimeOffset.UnixEpoch, Hash(), Tag(), ETag()));

    private static SyncBaselineEntry FolderEntry(string path) => SyncBaselineEntryFactory.Create(Account, PathOf(path), Id("item-" + path), SyncBaselineKindFactory.CreateFolder());

    private static RemoteItemId Id(string value) => RemoteItemIdFactory.Create(value).Match(id => id, _ => throw new InvalidOperationException());

    private static ContentHash Hash() => ContentHashFactory.Create("h").Match(hash => hash, _ => throw new InvalidOperationException());

    private static ContentTag Tag() => ContentTagFactory.Create("c").Match(tag => tag, _ => throw new InvalidOperationException());

    private static EntityTag ETag() => EntityTagFactory.Create("e").Match(tag => tag, _ => throw new InvalidOperationException());

    private static ClassifyingSyncBaselineWriter Build(ClassifyHookInnerWriter inner, ClassifyHookClassifier classifier, ClassifyHookStore store, ILogger<ClassifyingSyncBaselineWriter>? logger = null, IClassificationVocabularyProvider? vocabulary = null) =>
        new(inner, classifier, store, vocabulary ?? new RecordingVocabularyProvider(new Ok<ClassificationVocabulary, ClassificationFailure>(ClassificationVocabularyFactory.Create([], []))), logger ?? NullLogger<ClassifyingSyncBaselineWriter>.Instance);

    private static bool Succeeded(Result<Unit, ErrorResponse> result) => result.Match(_ => true, _ => false);

    [Fact]
    public async Task when_an_upload_shaped_single_file_is_written_then_it_is_classified_and_its_links_replaced()
    {
        var inner = new ClassifyHookInnerWriter();
        var store = new ClassifyHookStore();
        var writer = Build(inner, new ClassifyHookClassifier(), store);

        var result = await writer.UpsertAsync(Account, [FileEntry("Holiday/a.jpg")], TestContext.Current.CancellationToken);

        Succeeded(result).ShouldBeTrue();
        inner.Upserts.ShouldBe(1);
        store.Replaced.Select(links => links.Path.Value).ShouldBe(["Holiday/a.jpg"]);
        store.Replaced[0].AccountId.ShouldBe(Account);
        store.Replaced[0].TermIds.ShouldBe([ClassifyHookClassifier.TermId]);
    }

    [Fact]
    public async Task when_the_vocabulary_changes_while_a_batch_is_classified_then_the_batch_is_classified_and_written_again()
    {
        var vocabulary = new RecordingVocabularyProvider(new Ok<ClassificationVocabulary, ClassificationFailure>(ClassificationVocabularyFactory.Create([], [])));
        var classifier = new ClassifyHookClassifier { OnClassify = () => { if (vocabulary.Invalidations == 0) vocabulary.Invalidate(); } };
        var store = new ClassifyHookStore();
        var writer = Build(new ClassifyHookInnerWriter(), classifier, store, vocabulary: vocabulary);

        _ = await writer.UpsertAsync(Account, [FileEntry("a/1.jpg"), FileEntry("a/2.jpg")], TestContext.Current.CancellationToken);

        classifier.Paths.Count.ShouldBe(4);
        store.Replaced.Count.ShouldBe(4);
    }

    [Fact]
    public async Task when_the_vocabulary_keeps_changing_then_the_batch_is_not_reclassified_forever()
    {
        var vocabulary = new RecordingVocabularyProvider(new Ok<ClassificationVocabulary, ClassificationFailure>(ClassificationVocabularyFactory.Create([], [])));
        var classifier = new ClassifyHookClassifier { OnClassify = vocabulary.Invalidate };
        var writer = Build(new ClassifyHookInnerWriter(), classifier, new ClassifyHookStore(), vocabulary: vocabulary);

        var result = await writer.UpsertAsync(Account, [FileEntry("a/1.jpg")], TestContext.Current.CancellationToken);

        Succeeded(result).ShouldBeTrue();
        classifier.Paths.Count.ShouldBeLessThan(10);
    }

    [Fact]
    public async Task when_the_vocabulary_is_unchanged_then_the_batch_is_classified_once()
    {
        var classifier = new ClassifyHookClassifier();
        var writer = Build(new ClassifyHookInnerWriter(), classifier, new ClassifyHookStore());

        _ = await writer.UpsertAsync(Account, [FileEntry("a/1.jpg")], TestContext.Current.CancellationToken);

        classifier.Paths.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_download_shaped_batch_is_written_then_every_file_is_classified_in_one_store_write()
    {
        var store = new ClassifyHookStore();
        var writer = Build(new ClassifyHookInnerWriter(), new ClassifyHookClassifier(), store);

        _ = await writer.UpsertAsync(Account, [FileEntry("a/1.jpg"), FileEntry("a/2.jpg"), FileEntry("b/3.jpg")], TestContext.Current.CancellationToken);

        store.Replaced.Select(links => links.Path.Value).Order().ShouldBe(["a/1.jpg", "a/2.jpg", "b/3.jpg"]);
    }

    [Fact]
    public async Task when_a_folder_is_written_then_it_is_not_classified()
    {
        var classifier = new ClassifyHookClassifier();
        var store = new ClassifyHookStore();
        var writer = Build(new ClassifyHookInnerWriter(), classifier, store);

        _ = await writer.UpsertAsync(Account, [FolderEntry("Holiday"), FileEntry("Holiday/a.jpg")], TestContext.Current.CancellationToken);

        classifier.Paths.ShouldBe(["Holiday/a.jpg"]);
        store.Replaced.Select(links => links.Path.Value).ShouldBe(["Holiday/a.jpg"]);
    }

    [Fact]
    public async Task when_only_folders_are_written_then_the_store_is_not_called()
    {
        var store = new ClassifyHookStore();
        var writer = Build(new ClassifyHookInnerWriter(), new ClassifyHookClassifier(), store);

        _ = await writer.UpsertAsync(Account, [FolderEntry("Holiday")], TestContext.Current.CancellationToken);

        store.Replaced.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_file_is_renamed_then_the_old_path_is_removed_and_the_new_path_is_classified()
    {
        var store = new ClassifyHookStore();
        var writer = Build(new ClassifyHookInnerWriter(), new ClassifyHookClassifier(), store);

        _ = await writer.RemoveAsync(Account, [PathOf("old/a.jpg")], TestContext.Current.CancellationToken);
        _ = await writer.UpsertAsync(Account, [FileEntry("new/a.jpg")], TestContext.Current.CancellationToken);

        store.Removed.ShouldBe(["old/a.jpg"]);
        store.Replaced.Select(links => links.Path.Value).ShouldBe(["new/a.jpg"]);
    }

    [Fact]
    public async Task when_baseline_entries_are_removed_then_each_path_loses_its_links()
    {
        var inner = new ClassifyHookInnerWriter();
        var store = new ClassifyHookStore();
        var writer = Build(inner, new ClassifyHookClassifier(), store);

        var result = await writer.RemoveAsync(Account, [PathOf("a.jpg"), PathOf("b/c.jpg")], TestContext.Current.CancellationToken);

        Succeeded(result).ShouldBeTrue();
        inner.Removes.ShouldBe(1);
        store.Removed.Order().ShouldBe(["a.jpg", "b/c.jpg"]);
    }

    [Fact]
    public async Task when_the_inner_upsert_fails_then_the_failure_is_returned_and_nothing_is_classified()
    {
        var inner = new ClassifyHookInnerWriter { Outcome = Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")) };
        var classifier = new ClassifyHookClassifier();
        var store = new ClassifyHookStore();
        var writer = Build(inner, classifier, store);

        var result = await writer.UpsertAsync(Account, [FileEntry("a.jpg")], TestContext.Current.CancellationToken);

        result.ShouldBe(inner.Outcome);
        Succeeded(result).ShouldBeFalse();
        classifier.Paths.ShouldBeEmpty();
        store.Replaced.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_inner_remove_fails_then_the_failure_is_returned_and_no_links_are_removed()
    {
        var inner = new ClassifyHookInnerWriter { Outcome = Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")) };
        var store = new ClassifyHookStore();
        var writer = Build(inner, new ClassifyHookClassifier(), store);

        var result = await writer.RemoveAsync(Account, [PathOf("a.jpg")], TestContext.Current.CancellationToken);

        result.ShouldBe(inner.Outcome);
        store.Removed.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_classifier_throws_then_the_baseline_result_is_unchanged_and_a_warning_is_logged_without_the_message()
    {
        var inner = new ClassifyHookInnerWriter();
        var logger = new RecordingLogger<ClassifyingSyncBaselineWriter>();
        var classifier = new ClassifyHookClassifier { Throws = () => new InvalidOperationException("secret holiday.jpg term") };
        var writer = Build(inner, classifier, new ClassifyHookStore(), logger);

        var result = await writer.UpsertAsync(Account, [FileEntry("secret/holiday.jpg")], TestContext.Current.CancellationToken);

        result.ShouldBe(inner.Outcome);
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        logger.Messages.ShouldAllBe(message => !message.Contains("secret") && !message.Contains("holiday"));
        logger.Messages.ShouldContain(message => message.Contains(nameof(InvalidOperationException)));
    }

    [Fact]
    public async Task when_the_store_throws_then_the_baseline_result_is_unchanged_and_a_warning_is_logged()
    {
        var inner = new ClassifyHookInnerWriter();
        var logger = new RecordingLogger<ClassifyingSyncBaselineWriter>();
        var store = new ClassifyHookStore { Throws = () => new TimeoutException("db secret") };
        var writer = Build(inner, new ClassifyHookClassifier(), store, logger);

        var upsert = await writer.UpsertAsync(Account, [FileEntry("a.jpg")], TestContext.Current.CancellationToken);
        var remove = await writer.RemoveAsync(Account, [PathOf("a.jpg")], TestContext.Current.CancellationToken);

        upsert.ShouldBe(inner.Outcome);
        remove.ShouldBe(inner.Outcome);
        logger.Entries.Count(entry => entry.Level == LogLevel.Warning).ShouldBe(2);
        logger.Messages.ShouldAllBe(message => !message.Contains("secret"));
    }

    [Fact]
    public async Task when_the_store_returns_a_failure_then_the_baseline_result_is_unchanged_and_a_warning_is_logged()
    {
        var inner = new ClassifyHookInnerWriter();
        var logger = new RecordingLogger<ClassifyingSyncBaselineWriter>();
        var writer = Build(inner, new ClassifyHookClassifier(), new ClassifyHookStore { Fails = true }, logger);

        var upsert = await writer.UpsertAsync(Account, [FileEntry("a.jpg")], TestContext.Current.CancellationToken);
        var remove = await writer.RemoveAsync(Account, [PathOf("a.jpg")], TestContext.Current.CancellationToken);

        upsert.ShouldBe(inner.Outcome);
        remove.ShouldBe(inner.Outcome);
        logger.Entries.Count(entry => entry.Level == LogLevel.Warning).ShouldBe(2);
    }

    [Fact]
    public async Task when_the_sync_is_cancelled_during_classification_then_the_cancellation_passes_through()
    {
        using var source = new CancellationTokenSource();
        var classifier = new ClassifyHookClassifier { Throws = () => { source.Cancel(); return new OperationCanceledException(source.Token); } };
        var writer = Build(new ClassifyHookInnerWriter(), classifier, new ClassifyHookStore());

        var act = () => writer.UpsertAsync(Account, [FileEntry("a.jpg")], source.Token);

        _ = await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task when_the_token_is_cancelled_during_a_remove_then_the_cancellation_passes_through()
    {
        using var source = new CancellationTokenSource();
        var store = new ClassifyHookStore { Throws = () => { source.Cancel(); return new OperationCanceledException(source.Token); } };
        var writer = Build(new ClassifyHookInnerWriter(), new ClassifyHookClassifier(), store);

        var act = () => writer.RemoveAsync(Account, [PathOf("a.jpg")], source.Token);

        _ = await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task when_two_entries_differ_only_by_case_then_the_file_is_classified_once_under_the_last_spelling()
    {
        var classifier = new ClassifyHookClassifier();
        var store = new ClassifyHookStore();
        var writer = Build(new ClassifyHookInnerWriter(), classifier, store);

        _ = await writer.UpsertAsync(Account, [FileEntry("Holiday/A.JPG"), FileEntry("holiday/a.jpg")], TestContext.Current.CancellationToken);

        classifier.Paths.ShouldBe(["holiday/a.jpg"]);
        store.Replaced.Select(links => links.Path.Value).ShouldBe(["holiday/a.jpg"]);
    }

    [Fact]
    public async Task when_twenty_writes_run_in_parallel_then_every_file_is_classified_and_every_result_is_the_inner_result()
    {
        var inner = new ClassifyHookInnerWriter();
        var store = new ClassifyHookStore();
        var writer = Build(inner, new ClassifyHookClassifier(), store);

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(index => Task.Run(() => writer.UpsertAsync(Account, [FileEntry($"d{index}/f.jpg")], TestContext.Current.CancellationToken), TestContext.Current.CancellationToken)));

        results.ShouldAllBe(result => ReferenceEquals(result, inner.Outcome) || result.Equals(inner.Outcome));
        inner.Upserts.ShouldBe(20);
        store.Replaced.Select(links => links.Path.Value).Order().ShouldBe(Enumerable.Range(0, 20).Select(index => $"d{index}/f.jpg").Order());
    }

    [Fact]
    public async Task when_the_classifier_throws_then_the_warning_carries_the_error_type_as_its_own_value_and_a_fixed_message()
    {
        var logger = new RecordingLogger<ClassifyingSyncBaselineWriter>();
        var classifier = new ClassifyHookClassifier { Throws = () => new InvalidOperationException("secret holiday.jpg term") };
        var writer = Build(new ClassifyHookInnerWriter(), classifier, new ClassifyHookStore(), logger);

        await writer.UpsertAsync(Account, [FileEntry("secret/holiday.jpg")], TestContext.Current.CancellationToken);

        var values = logger.Events.ShouldHaveSingleItem().Values;
        (values["ErrorType"], values["Message"]).ShouldBe(("System.InvalidOperationException", "A file could not be classified after the baseline write"));
    }

    [Fact]
    public async Task when_the_store_returns_a_failure_then_the_warning_carries_the_failure_type_as_its_own_value_and_a_fixed_message()
    {
        var logger = new RecordingLogger<ClassifyingSyncBaselineWriter>();
        var writer = Build(new ClassifyHookInnerWriter(), new ClassifyHookClassifier(), new ClassifyHookStore { Fails = true }, logger);

        await writer.UpsertAsync(Account, [FileEntry("a.jpg")], TestContext.Current.CancellationToken);

        var values = logger.Events.ShouldHaveSingleItem().Values;
        values["Message"].ShouldBe("The classification links were not updated after the baseline write");
        values["ErrorType"].ShouldBeOfType<string>().ShouldNotBeEmpty();
    }
}
