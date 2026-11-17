using AStarDev.CloudSync.TestHelpers.Common;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenASyncStartup
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private readonly RecordingLogger<SyncStartup> logger = new();
    private readonly StartupFakeScheduler scheduler = new();

    private SyncStartup Create(StartupFakeClassification classification, StartupFakeScheduler syncScheduler) => new(classification, syncScheduler, logger);

    [Fact]
    public async Task when_the_seed_is_still_running_then_the_scheduler_has_already_started()
    {
        var classification = new StartupFakeClassification { HoldUntilReleased = true };
        var startup = Create(classification, scheduler);

        var run = startup.RunAsync(CancellationToken);
        var startedBeforeSeedFinished = scheduler.Starts == 1 && !classification.Finished;
        classification.Release();
        await run;

        startedBeforeSeedFinished.ShouldBeTrue();
        classification.Finished.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_seed_throws_then_the_scheduler_still_starts_and_a_warning_without_the_message_is_logged()
    {
        var classification = new StartupFakeClassification { Throws = new InvalidOperationException("user@example.com token-123 C:\\secret") };
        var startup = Create(classification, scheduler);

        await startup.RunAsync(CancellationToken);

        scheduler.Starts.ShouldBe(1);
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        logger.Messages.ShouldAllBe(message => !message.Contains("user@example.com") && !message.Contains("token-123") && !message.Contains("secret"));
    }

    [Fact]
    public async Task when_the_seed_is_cancelled_by_shutdown_then_run_completes_without_throwing_or_logging_a_warning()
    {
        using var shutdown = new CancellationTokenSource();
        var classification = new StartupFakeClassification { HoldUntilReleased = true };
        var startup = Create(classification, scheduler);

        var run = startup.RunAsync(shutdown.Token);
        await shutdown.CancelAsync();

        await Should.NotThrowAsync(() => run);
        scheduler.Starts.ShouldBe(1);
        logger.Entries.ShouldNotContain(entry => entry.Level >= LogLevel.Warning);
    }

    [Fact]
    public async Task when_the_application_is_already_shutting_down_then_the_scheduler_is_not_started()
    {
        using var shutdown = new CancellationTokenSource();
        await shutdown.CancelAsync();
        var classification = new StartupFakeClassification();
        var startup = Create(classification, scheduler);

        await Should.NotThrowAsync(() => startup.RunAsync(shutdown.Token));

        scheduler.Starts.ShouldBe(0);
        classification.Runs.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_scheduler_fails_to_start_then_the_failure_is_logged_by_type_and_the_seed_still_finishes()
    {
        var failing = new StartupFakeScheduler { Throws = true };
        var classification = new StartupFakeClassification();
        var startup = Create(classification, failing);

        await Should.NotThrowAsync(() => startup.RunAsync(CancellationToken));

        classification.Finished.ShouldBeTrue();
        logger.Entries.ShouldContain(entry => entry.Level >= LogLevel.Warning);
        logger.Messages.ShouldAllBe(message => !message.Contains("user@example.com") && !message.Contains("token-123"));
    }
}
