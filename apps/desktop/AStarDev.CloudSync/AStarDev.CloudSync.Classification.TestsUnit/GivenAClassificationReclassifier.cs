using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.Logging.Abstractions;
using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAClassificationReclassifier
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.NewGuid());
    private static readonly AccountId OtherAccount = AccountIdFactory.Create(Guid.NewGuid());
    private static readonly ClassificationTerm Holiday = Term("holiday", Place);

    private static ReclassifyOptions Batches(int size, int parallelism = 1, int delayMilliseconds = 0) => ReclassifyOptionsFactory.Create(size, parallelism, TimeSpan.FromMilliseconds(delayMilliseconds));

    private static ClassificationReclassifier Build(IClassifiableFileSource source, IFileClassifier classifier, IFileClassificationStore store, ReclassifyOptions? options = null) =>
        new(source, classifier, store, options ?? ReclassifyOptionsFactory.CreateDefault(), NullLogger<ClassificationReclassifier>.Instance);

    private static ReclassifyCounts Counts(ReclassifyOutcome outcome) => outcome switch
    {
        ReclassifyOutcome.Completed completed => completed.Counts,
        ReclassifyOutcome.Cancelled cancelled => cancelled.Counts,
        ReclassifyOutcome.Failed failed => failed.Counts,
        _ => throw new InvalidOperationException("The outcome carries no counts."),
    };

    private static (int, int, int) Totals(ReclassifyOutcome outcome)
    {
        var counts = Counts(outcome);

        return (counts.FilesWritten, counts.Batches, counts.DuplicatesSkipped);
    }

    [Fact]
    public async Task when_every_file_is_classified_then_progress_is_reported_after_each_batch()
    {
        var store = new ReclassifyFakeStore();
        var progress = new ReclassifyRecordingProgress();
        var reclassifier = Build(ReclassifyFakeFileSource.Numbered(Account, 1200), new ReclassifyFakeClassifier(Holiday), store);

        var outcome = await reclassifier.StartAsync(progress, TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ReclassifyOutcome.Completed>();
        Totals(outcome).ShouldBe((1200, 3, 0));
        progress.Reports.ShouldBe([(500, 1), (1000, 2), (1200, 3)]);
        store.BatchSizes.ShouldBe([500, 500, 200]);
    }

    [Fact]
    public async Task when_a_file_matches_nothing_then_its_links_are_replaced_with_none()
    {
        var store = new ReclassifyFakeStore();
        var source = new ReclassifyFakeFileSource([ClassifiableFileFactory.Create(Account, PathOf("misc/a.bin")), ClassifiableFileFactory.Create(Account, PathOf("Holiday/b.jpg"))]);
        var reclassifier = Build(source, new ReclassifyFakeClassifier(Holiday), store);

        _ = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        store.Links[(Account, PathOf("misc/a.bin"))].ShouldBeEmpty();
        store.Links[(Account, PathOf("Holiday/b.jpg"))].ShouldBe([Holiday.Id]);
    }

    [Fact]
    public async Task when_the_baseline_is_empty_then_the_run_completes_with_nothing_written()
    {
        var reclassifier = Build(new ReclassifyFakeFileSource([]), new ReclassifyFakeClassifier(Holiday), new ReclassifyFakeStore());
        var progress = new ReclassifyRecordingProgress();

        var outcome = await reclassifier.StartAsync(progress, TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ReclassifyOutcome.Completed>();
        Totals(outcome).ShouldBe((0, 0, 0));
        progress.Reports.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_run_is_cancelled_mid_batch_then_completed_batches_stay_and_the_current_batch_is_not_written()
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var store = new ReclassifyFakeStore();
        var classifier = new ReclassifyFakeClassifier(Holiday) { AfterClassified = count => { if (count == 700) source.Cancel(); } };
        var reclassifier = Build(ReclassifyFakeFileSource.Numbered(Account, 1200), classifier, store);

        var outcome = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), source.Token);

        outcome.ShouldBeOfType<ReclassifyOutcome.Cancelled>();
        Totals(outcome).ShouldBe((500, 1, 0));
        store.Links.Count.ShouldBe(500);
        store.BatchSizes.ShouldBe([500]);
    }

    [Fact]
    public async Task when_the_token_is_already_cancelled_then_nothing_is_written_and_the_outcome_is_cancelled()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        var store = new ReclassifyFakeStore();
        var reclassifier = Build(ReclassifyFakeFileSource.Numbered(Account, 10), new ReclassifyFakeClassifier(Holiday), store);

        var outcome = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), source.Token);

        outcome.ShouldBeOfType<ReclassifyOutcome.Cancelled>();
        Totals(outcome).ShouldBe((0, 0, 0));
        store.Links.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_run_is_repeated_then_the_stored_links_are_the_same()
    {
        var store = new ReclassifyFakeStore();
        var reclassifier = Build(ReclassifyFakeFileSource.Numbered(Account, 700), new ReclassifyFakeClassifier(Holiday), store);
        _ = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);
        var first = store.Links.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());

        var second = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        second.ShouldBeOfType<ReclassifyOutcome.Completed>();
        Totals(second).ShouldBe((700, 2, 0));
        store.Links.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()).ShouldBe(first);
    }

    [Fact]
    public async Task when_the_stream_fails_then_the_run_fails_and_earlier_batches_stay()
    {
        var store = new ReclassifyFakeStore();
        var failing = new ReclassifyFakeFileSource([.. Enumerable.Range(0, 1200).Select(index => ClassifiableFileFactory.Create(Account, PathOf($"Holiday/f-{index:D5}.jpg")))]) { FailAfter = 700 };
        var reclassifier = Build(failing, new ReclassifyFakeClassifier(Holiday), store);

        var outcome = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ReclassifyOutcome.Failed>();
        Totals(outcome).ShouldBe((500, 1, 0));
        store.Links.Count.ShouldBe(500);
    }

    [Fact]
    public async Task when_a_batch_write_fails_then_earlier_batches_stay_and_the_run_stops()
    {
        var store = new ReclassifyFakeStore { FailOnBatch = 2 };
        var reclassifier = Build(ReclassifyFakeFileSource.Numbered(Account, 1200), new ReclassifyFakeClassifier(Holiday), store);

        var outcome = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ReclassifyOutcome.Failed>();
        Totals(outcome).ShouldBe((500, 1, 0));
        store.Links.Count.ShouldBe(500);
    }

    [Fact]
    public async Task when_the_same_path_appears_with_a_different_case_then_it_is_written_once_per_account()
    {
        var store = new ReclassifyFakeStore();
        var source = new ReclassifyFakeFileSource([
            ClassifiableFileFactory.Create(Account, PathOf("Holiday/Beach.JPG")),
            ClassifiableFileFactory.Create(Account, PathOf("holiday/beach.jpg")),
            ClassifiableFileFactory.Create(Account, PathOf("Holiday/Beach.JPG")),
            ClassifiableFileFactory.Create(OtherAccount, PathOf("holiday/beach.jpg")),
        ]);
        var reclassifier = Build(source, new ReclassifyFakeClassifier(Holiday), store);

        var outcome = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        Totals(outcome).ShouldBe((2, 1, 2));
        store.Links.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_a_run_is_active_then_about_twenty_parallel_callers_get_already_running_and_one_run_completes()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new ReclassifyFakeFileSource([ClassifiableFileFactory.Create(Account, PathOf("Holiday/a.jpg"))]) { BlockUntil = release.Task };
        var reclassifier = Build(source, new ReclassifyFakeClassifier(Holiday), new ReclassifyFakeStore());
        var calls = Enumerable.Range(0, 20).Select(_ => Task.Run(() => reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken), TestContext.Current.CancellationToken)).ToArray();
        await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        while (calls.Count(call => call.IsCompleted) < 19)
            await Task.Delay(5, TestContext.Current.CancellationToken);

        release.SetResult();
        var outcomes = await Task.WhenAll(calls);

        outcomes.Count(outcome => outcome is ReclassifyOutcome.AlreadyRunning).ShouldBe(19);
        outcomes.Count(outcome => outcome is ReclassifyOutcome.Completed).ShouldBe(1);
    }

    [Fact]
    public async Task when_a_run_has_ended_then_a_new_run_can_start()
    {
        var reclassifier = Build(ReclassifyFakeFileSource.Numbered(Account, 3), new ReclassifyFakeClassifier(Holiday), new ReclassifyFakeStore());
        _ = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        var outcome = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ReclassifyOutcome.Completed>();
    }

    [Fact]
    public async Task when_files_are_classified_in_parallel_with_a_delay_then_every_file_is_written()
    {
        var store = new ReclassifyFakeStore();
        var reclassifier = Build(ReclassifyFakeFileSource.Numbered(Account, 90), new ReclassifyFakeClassifier(Holiday), store, Batches(40, 4, 1));

        var outcome = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        Totals(outcome).ShouldBe((90, 3, 0));
        store.Links.Count.ShouldBe(90);
    }

    [Fact]
    public void when_options_are_invalid_then_they_are_normalised()
    {
        var options = ReclassifyOptionsFactory.Create(0, -3, TimeSpan.FromSeconds(-1));

        (options.BatchSize, options.DegreeOfParallelism, options.DelayBetweenBatches).ShouldBe((500, 1, TimeSpan.Zero));
    }

    [Fact]
    public void when_default_options_are_created_then_the_batch_is_500_and_parallelism_is_one()
    {
        var options = ReclassifyOptionsFactory.CreateDefault();

        (options.BatchSize, options.DegreeOfParallelism).ShouldBe((500, 1));
    }
}
