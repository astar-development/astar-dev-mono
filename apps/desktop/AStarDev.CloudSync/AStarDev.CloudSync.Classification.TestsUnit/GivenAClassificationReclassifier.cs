using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;
using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAClassificationReclassifier
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.NewGuid());
    private static readonly AccountId OtherAccount = AccountIdFactory.Create(Guid.NewGuid());
    private static readonly ClassificationTerm Holiday = Term("holiday", Place);

    private static ReclassifyOptions Batches(int size, int parallelism = 1, int delayMilliseconds = 0) => ReclassifyOptionsFactory.Create(size, parallelism, TimeSpan.FromMilliseconds(delayMilliseconds));

    private static ClassificationReclassifier Build(IClassifiableFileSource source, IFileClassifier classifier, IFileClassificationStore store, ReclassifyOptions? options = null, IAccountSyncLock? syncLock = null, IAccountStore? accounts = null) =>
        new(source, classifier, store, syncLock ?? AccountSyncLocks.Create(), accounts ?? Known(Account, OtherAccount), options ?? ReclassifyOptionsFactory.CreateDefault(), NullLogger<ClassificationReclassifier>.Instance);

    private static AccountRegistration Registration(string key) =>
        AccountRegistrationFactory.Create(ProviderAccountKeyFactory.Create(key).Match(created => created, _ => throw new InvalidOperationException("The test key must be valid.")), CloudProviderFactory.CreateOneDrive(), key, $"{key}@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1, 2));

    private static FakeAccountStore Known(params AccountId[] accountIds) =>
        new() { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([.. accountIds.Select(accountId => StoredAccountFactory.Create(accountId, Registration(accountId.Value.ToString("N"))))]) };

    private static ReclassifyFakeFileSource ThreeFiles() => new([
        ClassifiableFileFactory.Create(Account, PathOf("Holiday/a.jpg")),
        ClassifiableFileFactory.Create(OtherAccount, PathOf("Holiday/b.jpg")),
        ClassifiableFileFactory.Create(Account, PathOf("Holiday/c.jpg")),
    ]);

    private static async Task RemoveUnderLeaseAsync(IAccountSyncLock syncLock, InMemoryAccountStore accounts, ReclassifyFakeStore store, AccountId accountId, CancellationToken cancellationToken)
    {
        var attempt = syncLock.TryAcquire(accountId);
        while (attempt is not SyncLockAttempt.Acquired)
        {
            await Task.Yield();
            attempt = syncLock.TryAcquire(accountId);
        }

        using var lease = ((SyncLockAttempt.Acquired)attempt).Lease;
        store.RemoveAccount(accountId);
        _ = await accounts.RemoveAsync(accountId, cancellationToken);
    }

    private static ReclassifyCounts Counts(ReclassifyOutcome outcome) => outcome switch
    {
        ReclassifyOutcome.Completed completed => completed.Counts,
        ReclassifyOutcome.Cancelled cancelled => cancelled.Counts,
        ReclassifyOutcome.Failed failed => failed.Counts,
        _ => throw new InvalidOperationException("The outcome carries no counts."),
    };

    private static string[] Snapshot(ReclassifyFakeStore store) => [.. store.Links.Select(pair => $"{pair.Key.Item1.Value}|{pair.Key.Item2.Value}|{string.Join(',', pair.Value.Select(term => term.Value))}").Order()];

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
        progress.Reports.ShouldBe([(500, 1, 0), (1000, 2, 0), (1200, 3, 0)]);
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
        var first = Snapshot(store);

        var second = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        second.ShouldBeOfType<ReclassifyOutcome.Completed>();
        Totals(second).ShouldBe((700, 2, 0));
        Snapshot(store).ShouldBe(first);
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
    public async Task when_no_run_is_active_then_waiting_until_idle_completes_at_once()
    {
        var reclassifier = Build(ThreeFiles(), new ReclassifyFakeClassifier(Holiday), new ReclassifyFakeStore());

        var waiting = reclassifier.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        await waiting.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        waiting.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_run_is_active_then_waiting_until_idle_completes_only_after_the_run_ended_and_a_new_run_can_start()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new ReclassifyFakeFileSource([ClassifiableFileFactory.Create(Account, PathOf("Holiday/a.jpg"))]) { BlockUntil = release.Task };
        var reclassifier = Build(source, new ReclassifyFakeClassifier(Holiday), new ReclassifyFakeStore());
        var run = reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);
        await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var waiting = reclassifier.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        var completedWhileRunning = waiting.IsCompleted;

        release.SetResult();
        await waiting.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        _ = await run;
        var next = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        completedWhileRunning.ShouldBeFalse();
        next.ShouldBeOfType<ReclassifyOutcome.Completed>();
    }

    [Fact]
    public async Task when_the_wait_is_cancelled_while_a_run_is_active_then_it_returns_without_throwing_and_the_run_is_unaffected()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new ReclassifyFakeFileSource([ClassifiableFileFactory.Create(Account, PathOf("Holiday/a.jpg"))]) { BlockUntil = release.Task };
        var reclassifier = Build(source, new ReclassifyFakeClassifier(Holiday), new ReclassifyFakeStore());
        var run = reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);
        await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        using var cancel = new CancellationTokenSource();
        var waiting = reclassifier.WaitUntilIdleAsync(cancel.Token);

        await cancel.CancelAsync();
        await waiting.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        release.SetResult();

        (await run).ShouldBeOfType<ReclassifyOutcome.Completed>();
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

        (options.BatchSize, options.DegreeOfParallelism, options.DelayBetweenBatches).ShouldBe((500, 1, TimeSpan.FromMilliseconds(5)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(4)]
    public void when_the_pause_between_batches_is_below_the_minimum_then_it_is_raised_to_five_milliseconds(int milliseconds)
    {
        var options = ReclassifyOptionsFactory.Create(10, 1, TimeSpan.FromMilliseconds(milliseconds));

        options.DelayBetweenBatches.ShouldBe(TimeSpan.FromMilliseconds(5));
    }

    [Fact]
    public void when_the_pause_between_batches_is_above_the_minimum_then_it_is_kept()
    {
        var options = ReclassifyOptionsFactory.Create(10, 1, TimeSpan.FromMilliseconds(50));

        options.DelayBetweenBatches.ShouldBe(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public void when_default_options_are_created_then_the_batch_is_500_and_parallelism_is_one()
    {
        var options = ReclassifyOptionsFactory.CreateDefault();

        (options.BatchSize, options.DegreeOfParallelism).ShouldBe((500, 1));
    }

    [Fact]
    public void when_default_options_are_created_then_a_pause_follows_each_batch_so_a_running_sync_is_not_starved()
    {
        var options = ReclassifyOptionsFactory.CreateDefault();

        options.DelayBetweenBatches.ShouldBe(TimeSpan.FromMilliseconds(20));
    }

    [Fact]
    public async Task when_another_pass_holds_an_accounts_lease_then_its_files_are_skipped_and_counted_and_the_other_account_is_written()
    {
        var syncLock = AccountSyncLocks.Create();
        using var held = syncLock.TryAcquire(Account).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        var store = new ReclassifyFakeStore();
        var reclassifier = Build(ThreeFiles(), new ReclassifyFakeClassifier(Holiday), store, syncLock: syncLock);

        var outcome = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ReclassifyOutcome.Completed>();
        Totals(outcome).ShouldBe((1, 1, 0));
        Counts(outcome).SkippedForAccount.ShouldBe(2);
        store.Links.Keys.ShouldBe([(OtherAccount, PathOf("Holiday/b.jpg"))]);
    }

    [Fact]
    public async Task when_a_batch_write_holds_the_lease_then_a_user_action_waits_for_that_write_instead_of_bouncing()
    {
        var syncLock = AccountSyncLocks.Create();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new ReclassifyFakeStore { HoldWrite = release.Task };
        var reclassifier = Build(ThreeFiles(), new ReclassifyFakeClassifier(Holiday), store, syncLock: syncLock);
        var run = reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);
        await store.WriteEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

        var userAction = syncLock.AcquireAsync(Account, TestContext.Current.CancellationToken);
        var waitedForTheWrite = !userAction.IsCompleted;
        release.SetResult();
        var attempt = await userAction;

        using var lease = attempt.ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        waitedForTheWrite.ShouldBeTrue();
        _ = await run;
    }

    [Fact]
    public async Task when_a_batch_finds_an_accounts_lease_taken_then_the_run_backs_off_for_the_longer_contention_pause()
    {
        var syncLock = AccountSyncLocks.Create();
        using var pass = syncLock.TryAcquire(Account).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        var time = new DelayRecordingTimeProvider();
        var options = ReclassifyOptionsFactory.Create(1, 1, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(500), time);
        var reclassifier = Build(ThreeFiles(), new ReclassifyFakeClassifier(Holiday), new ReclassifyFakeStore(), options, syncLock);
        var run = reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);
        var delays = new List<TimeSpan>();

        for (var batch = 0; batch < 3; batch++)
        {
            var delay = await time.NextDelayAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
            delays.Add(delay);
            time.Advance(delay);
        }

        var outcome = await run;

        delays.ShouldBe([TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(500)]);
        Counts(outcome).SkippedForAccount.ShouldBe(2);
    }

    [Fact]
    public void when_options_are_created_with_a_negative_contention_pause_then_it_becomes_none()
    {
        var options = ReclassifyOptionsFactory.Create(10, 1, TimeSpan.Zero, TimeSpan.FromSeconds(-1), TimeProvider.System);

        options.DelayAfterContention.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void when_default_options_are_created_then_the_contention_pause_is_longer_than_the_lease_retry_interval()
    {
        var options = ReclassifyOptionsFactory.CreateDefault();

        (options.DelayAfterContention > AccountLeasePolicy.RetryInterval, options.TimeProvider).ShouldBe((true, TimeProvider.System));
    }

    [Fact]
    public async Task when_an_account_has_been_removed_then_its_files_are_skipped_and_counted_and_no_links_are_written_for_it()
    {
        var store = new ReclassifyFakeStore();
        var reclassifier = Build(ThreeFiles(), new ReclassifyFakeClassifier(Holiday), store, accounts: Known(OtherAccount));

        var outcome = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ReclassifyOutcome.Completed>();
        Totals(outcome).ShouldBe((1, 1, 0));
        Counts(outcome).SkippedForAccount.ShouldBe(2);
        store.Links.Keys.ShouldBe([(OtherAccount, PathOf("Holiday/b.jpg"))]);
    }

    [Fact]
    public async Task when_the_lock_is_drained_then_every_file_is_skipped_and_the_skipped_batch_still_reports_progress()
    {
        var syncLock = AccountSyncLocks.Create();
        using var drain = await syncLock.DrainAsync(TestContext.Current.CancellationToken);
        var store = new ReclassifyFakeStore();
        var progress = new ReclassifyRecordingProgress();
        var reclassifier = Build(ThreeFiles(), new ReclassifyFakeClassifier(Holiday), store, syncLock: syncLock);

        var outcome = await reclassifier.StartAsync(progress, TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ReclassifyOutcome.Completed>();
        Totals(outcome).ShouldBe((0, 0, 0));
        Counts(outcome).SkippedForAccount.ShouldBe(3);
        store.BatchSizes.ShouldBeEmpty();
        progress.Reports.ShouldBe([(0, 0, 3)]);
    }

    [Fact]
    public async Task when_every_batch_of_an_account_is_skipped_then_progress_is_reported_after_each_batch_with_the_skipped_count()
    {
        var syncLock = AccountSyncLocks.Create();
        using var held = syncLock.TryAcquire(Account).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        var progress = new ReclassifyRecordingProgress();
        var reclassifier = Build(ReclassifyFakeFileSource.Numbered(Account, 1200), new ReclassifyFakeClassifier(Holiday), new ReclassifyFakeStore(), syncLock: syncLock);

        var outcome = await reclassifier.StartAsync(progress, TestContext.Current.CancellationToken);

        Counts(outcome).SkippedForAccount.ShouldBe(1200);
        progress.Reports.ShouldBe([(0, 0, 500), (0, 0, 1000), (0, 0, 1200)]);
    }

    [Fact]
    public async Task when_a_batch_is_partly_skipped_then_its_progress_carries_the_written_and_skipped_counts()
    {
        var syncLock = AccountSyncLocks.Create();
        using var held = syncLock.TryAcquire(Account).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        var progress = new ReclassifyRecordingProgress();
        var reclassifier = Build(ThreeFiles(), new ReclassifyFakeClassifier(Holiday), new ReclassifyFakeStore(), syncLock: syncLock);

        _ = await reclassifier.StartAsync(progress, TestContext.Current.CancellationToken);

        progress.Reports.ShouldBe([(1, 1, 2)]);
    }

    [Fact]
    public async Task when_the_accounts_cannot_be_read_then_the_run_fails_and_nothing_is_written()
    {
        var store = new ReclassifyFakeStore();
        var accounts = new FakeAccountStore { All = () => Result.Failure<IReadOnlyList<StoredAccount>, ErrorResponse>(new ErrorResponse("The accounts could not be read.")) };
        var reclassifier = Build(ThreeFiles(), new ReclassifyFakeClassifier(Holiday), store, accounts: accounts);

        var outcome = await reclassifier.StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ReclassifyOutcome.Failed>();
        store.Links.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_run_has_ended_then_every_account_lease_is_released()
    {
        var syncLock = AccountSyncLocks.Create();
        var accounts = new FakeAccountStore { All = () => Result.Failure<IReadOnlyList<StoredAccount>, ErrorResponse>(new ErrorResponse("The accounts could not be read.")) };
        _ = await Build(ThreeFiles(), new ReclassifyFakeClassifier(Holiday), new ReclassifyFakeStore(), syncLock: syncLock).StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);
        _ = await Build(ThreeFiles(), new ReclassifyFakeClassifier(Holiday), new ReclassifyFakeStore(), syncLock: syncLock, accounts: accounts).StartAsync(new ReclassifyRecordingProgress(), TestContext.Current.CancellationToken);

        using var first = syncLock.TryAcquire(Account).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
        using var second = syncLock.TryAcquire(OtherAccount).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;
    }

    [Fact]
    public async Task when_about_twenty_accounts_are_removed_in_parallel_with_a_run_then_no_links_remain_for_any_of_them()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var syncLock = AccountSyncLocks.Create();
        var accounts = new InMemoryAccountStore();
        var accountIds = new List<AccountId>();
        foreach (var index in Enumerable.Range(0, 20))
            accountIds.Add((await accounts.AddAsync(Registration($"account-{index}"), cancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId);

        var files = accountIds.SelectMany(accountId => Enumerable.Range(0, 25).Select(index => ClassifiableFileFactory.Create(accountId, PathOf($"Holiday/file-{index:D3}.jpg")))).ToList();
        var store = new ReclassifyFakeStore();
        var progress = new RemovalRacingProgress(50);
        var reclassifier = Build(new ReclassifyFakeFileSource(files), new ReclassifyFakeClassifier(Holiday), store, Batches(5), syncLock, accounts);
        var run = Task.Run(() => reclassifier.StartAsync(progress, cancellationToken), cancellationToken);
        await progress.FirstBatchReported.Task.WaitAsync(cancellationToken);
        var removals = accountIds.Select(accountId => Task.Run(() => RemoveUnderLeaseAsync(syncLock, accounts, store, accountId, cancellationToken), cancellationToken)).ToArray();

        await Task.WhenAll(removals);
        progress.RemovalsDone.SetResult();
        var outcome = await run;

        outcome.ShouldBeOfType<ReclassifyOutcome.Completed>();
        (Counts(outcome).FilesWritten + Counts(outcome).SkippedForAccount).ShouldBe(500);
        store.Links.ShouldBeEmpty();
    }
}
