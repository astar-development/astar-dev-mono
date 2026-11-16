using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAClassificationStartup
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private readonly List<string> calls = [];
    private readonly RecordingLogger<ClassificationStartup> logger = new();

    private ClassificationStartup Create(StartupFakeDatabase database, StartupFakeSeeder seeder) => new(database, seeder, logger);

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
}
