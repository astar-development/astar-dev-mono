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

    private ClassificationStartup Create(ICloudSyncDatabase database, IClassificationSeeder seeder) => new(database, seeder, reclassifier, logger);

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
}
