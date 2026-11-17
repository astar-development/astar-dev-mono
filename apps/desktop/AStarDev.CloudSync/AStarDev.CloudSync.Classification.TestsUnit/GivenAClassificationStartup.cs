using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAClassificationStartup
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private readonly List<string> calls = [];
    private readonly RecordingLogger<ClassificationStartup> logger = new();
    private readonly FakeClassificationReclassifier reclassifier = new();

    private readonly InMemoryReclassifyRunStore runs = new() { Last = Option.Some(ReclassifyRunRecordFactory.Create(ClassificationEngineVersion.Current, Option.Some("v1"), DateTimeOffset.UnixEpoch)) };

    private ClassificationStartup Create(ICloudSyncDatabase database, IClassificationSeeder seeder) => new(database, seeder, reclassifier, runs, logger);

    [Fact]
    public async Task when_run_then_the_database_is_migrated_before_the_seed_is_applied()
    {
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls));

        await startup.RunAsync(CancellationToken);

        calls.ShouldBe(["migrate", "seed"]);
    }

    [Fact]
    public async Task when_the_migration_fails_then_nothing_is_seeded_and_a_warning_is_logged_without_throwing()
    {
        var failing = new StartupFakeDatabase(calls) { Outcome = Result.Failure<Unit, ErrorResponse>(new ErrorResponse("C:\\secret\\path is locked")) };
        var startup = Create(failing, new StartupFakeSeeder(calls));

        await startup.RunAsync(CancellationToken);

        calls.ShouldBe(["migrate"]);
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        logger.Messages.ShouldAllBe(message => !message.Contains("secret"));
    }

    [Fact]
    public async Task when_the_seed_throws_then_the_failure_is_logged_without_its_message_and_run_completes()
    {
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls) { Throws = true });

        await startup.RunAsync(CancellationToken);

        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        logger.Messages.ShouldAllBe(message => !message.Contains("secret"));
    }

    [Fact]
    public async Task when_the_seed_outcome_is_a_failure_then_run_completes_without_throwing()
    {
        var failed = ClassificationSeedOutcomeFactory.CreateFailed(ClassificationFailureFactory.CreateUnavailable());
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls) { Outcome = failed });

        await startup.RunAsync(CancellationToken);

        calls.ShouldBe(["migrate", "seed"]);
    }

    [Fact]
    public async Task when_cancelled_then_run_completes_without_throwing()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls) { Throws = true });

        await Should.NotThrowAsync(() => startup.RunAsync(cancelled.Token));
    }

    [Fact]
    public async Task when_the_seed_throws_after_the_token_was_cancelled_then_no_warning_is_logged()
    {
        using var cancelled = new CancellationTokenSource();
        var startup = Create(new StartupFakeDatabase(calls), new CancellingSeeder(cancelled));

        await startup.RunAsync(cancelled.Token);

        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_migration_fails_after_the_token_was_cancelled_then_no_warning_is_logged()
    {
        using var cancelled = new CancellationTokenSource();
        var failing = new CancellingDatabase(cancelled);
        var startup = Create(failing, new StartupFakeSeeder(calls));

        await startup.RunAsync(cancelled.Token);

        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_seed_is_applied_then_the_baseline_is_reclassified_once()
    {
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls));

        await startup.RunAsync(CancellationToken);

        reclassifier.StartCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_startup_reclassify_gets_already_running_then_it_waits_for_the_active_run_and_starts_again()
    {
        reclassifier.AlreadyRunningBeforeSuccess = 1;
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls));

        await startup.RunAsync(CancellationToken);

        reclassifier.Calls.ShouldBe(["start", "wait", "start"]);
    }

    [Fact]
    public async Task when_a_manual_run_keeps_winning_the_gap_then_the_startup_reclassify_keeps_waiting_until_it_gets_a_run()
    {
        reclassifier.AlreadyRunningBeforeSuccess = 3;
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls));

        await startup.RunAsync(CancellationToken);

        reclassifier.StartCount.ShouldBe(4);
        reclassifier.WaitCount.ShouldBe(3);
    }

    [Fact]
    public async Task when_shutdown_is_requested_while_waiting_for_the_active_run_then_run_stops_waiting_without_a_warning()
    {
        using var shutdown = new CancellationTokenSource();
        reclassifier.AlreadyRunningBeforeSuccess = int.MaxValue;
        reclassifier.OnWait = shutdown.Cancel;
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls));

        await startup.RunAsync(shutdown.Token).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);

        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_seed_is_already_current_then_nothing_is_reclassified()
    {
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls) { Outcome = ClassificationSeedOutcomeFactory.CreateAlreadyCurrent() });

        await startup.RunAsync(CancellationToken);

        reclassifier.StartCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_seed_outcome_is_a_failure_then_nothing_is_reclassified()
    {
        var failed = ClassificationSeedOutcomeFactory.CreateFailed(ClassificationFailureFactory.CreateUnavailable());
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls) { Outcome = failed });

        await startup.RunAsync(CancellationToken);

        reclassifier.StartCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_migration_fails_then_nothing_is_reclassified()
    {
        var failing = new StartupFakeDatabase(calls) { Outcome = Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")) };
        var startup = Create(failing, new StartupFakeSeeder(calls));

        await startup.RunAsync(CancellationToken);

        reclassifier.StartCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_shutdown_is_requested_during_the_reclassify_then_run_completes_without_a_warning()
    {
        using var shutdown = new CancellationTokenSource();
        reclassifier.Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls));

        var run = startup.RunAsync(shutdown.Token);
        await Task.WhenAny(reclassifier.Started.Task, run);
        var startedBeforeShutdown = reclassifier.Started.Task.IsCompleted;
        var receivedShutdownToken = reclassifier.ReceivedToken == shutdown.Token;
        await shutdown.CancelAsync();
        _ = reclassifier.Gate.TrySetResult();
        await run.WaitAsync(CancellationToken);

        startedBeforeShutdown.ShouldBeTrue();
        receivedShutdownToken.ShouldBeTrue();
        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_reclassify_fails_then_run_completes_without_throwing()
    {
        reclassifier.Outcome = ReclassifyOutcomeFactory.CreateFailed(ReclassifyCountsFactory.Create(0, 0, 0));
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls));

        await Should.NotThrowAsync(() => startup.RunAsync(CancellationToken));

        reclassifier.StartCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_reclassify_throws_then_a_warning_is_logged_without_its_message_and_run_completes()
    {
        reclassifier.Throws = true;
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls));

        await startup.RunAsync(CancellationToken);

        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        logger.Messages.ShouldAllBe(message => !message.Contains("secret"));
    }

    [Fact]
    public async Task when_the_seed_is_already_current_and_no_run_was_ever_recorded_then_the_baseline_is_reclassified_once()
    {
        runs.Last = Option.None<ReclassifyRunRecord>();
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls) { Outcome = ClassificationSeedOutcomeFactory.CreateAlreadyCurrent() });

        await startup.RunAsync(CancellationToken);

        reclassifier.StartCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_seed_is_applied_and_no_run_was_ever_recorded_then_the_baseline_is_reclassified_once()
    {
        runs.Last = Option.None<ReclassifyRunRecord>();
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls));

        await startup.RunAsync(CancellationToken);

        reclassifier.StartCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_run_record_cannot_be_read_and_the_seed_is_already_current_then_nothing_is_reclassified()
    {
        var unreadable = new InMemoryReclassifyRunStore { FailReads = true };
        var startup = new ClassificationStartup(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls) { Outcome = ClassificationSeedOutcomeFactory.CreateAlreadyCurrent() }, reclassifier, unreadable, logger);

        await startup.RunAsync(CancellationToken);

        reclassifier.StartCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_seed_failed_and_no_run_was_ever_recorded_then_nothing_is_reclassified()
    {
        runs.Last = Option.None<ReclassifyRunRecord>();
        var failed = ClassificationSeedOutcomeFactory.CreateFailed(ClassificationFailureFactory.CreateUnavailable());
        var startup = Create(new StartupFakeDatabase(calls), new StartupFakeSeeder(calls) { Outcome = failed });

        await startup.RunAsync(CancellationToken);

        reclassifier.StartCount.ShouldBe(0);
    }
}
