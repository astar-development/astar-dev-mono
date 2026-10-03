using AStarDev.WallpaperScraper.Operations;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.Startup;
using AStarDev.WallpaperScraper.UI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.WallpaperScraper.TestsUnit.UI;

public sealed class GivenApplicationReadiness : IDisposable
{
    private readonly FakeDatabaseInitialization database = new();
    private readonly FakeRootDirectoryCheck rootDirectoryCheck = new();
    private readonly OperationCoordinator coordinator = new();
    private readonly StatusReporter status = TestStatusReporter.Create();
    private readonly ApplicationReadiness readiness;

    public GivenApplicationReadiness() => readiness = new(database, rootDirectoryCheck, coordinator, status);

    public void Dispose() => coordinator.Dispose();

    [Fact]
    public void when_nothing_has_been_initialised_then_nothing_can_be_operated_on()
        => (readiness.CanOperate, readiness.CanRunScraper).ShouldBe((false, false));

    [Fact]
    public async Task when_the_database_is_ready_and_the_root_directory_exists_then_operations_and_the_scraper_are_enabled()
    {
        var ready = await readiness.InitialiseAsync();

        (ready, readiness.CanOperate, readiness.CanRunScraper, status.Text).ShouldBe((true, true, true, $"{TestStatusReporter.Timestamp} Preparing the database.{Environment.NewLine}{TestStatusReporter.Timestamp} Database ready."));
    }

    [Fact]
    public async Task when_the_root_directory_does_not_exist_then_operations_are_enabled_but_the_scraper_is_not_and_the_user_is_told()
    {
        rootDirectoryCheck.Exists = false;

        await readiness.InitialiseAsync();

        (readiness.CanOperate, readiness.CanRunScraper, status.Text.Contains("Root directory could not be found.", StringComparison.Ordinal)).ShouldBe((true, false, true));
    }

    [Fact]
    public async Task when_the_database_fails_to_prepare_then_nothing_is_enabled_and_the_failure_is_reported()
    {
        database.Ready = Task.FromException(new IOException("disk full"));

        var ready = await readiness.InitialiseAsync();

        (ready, readiness.CanOperate, readiness.CanRunScraper, status.Text.Contains("Unable to prepare the database. disk full", StringComparison.Ordinal)).ShouldBe((false, false, false, true));
    }

    [Fact]
    public async Task when_checking_the_root_directory_fails_then_the_failure_is_reported_and_the_scraper_is_not_enabled()
    {
        rootDirectoryCheck.Check = () => throw new InvalidOperationException("Scrape configuration not found");

        await readiness.InitialiseAsync();

        (readiness.CanOperate, readiness.CanRunScraper, status.Text.Contains("Unable to check the root directory. Scrape configuration not found", StringComparison.Ordinal)).ShouldBe((true, false, true));
    }

    [Fact]
    public async Task when_an_operation_is_running_then_operations_and_the_scraper_are_disabled_and_changed_is_raised()
    {
        await readiness.InitialiseAsync();
        var changes = 0;
        readiness.Changed += (_, _) => changes++;

        _ = coordinator.TryStart(out _);

        (readiness.CanOperate, readiness.CanRunScraper, changes).ShouldBe((false, false, 1));
    }

    [Fact]
    public async Task when_initialisation_completes_then_changed_is_raised()
    {
        var changes = 0;
        readiness.Changed += (_, _) => changes++;

        await readiness.InitialiseAsync();

        (changes > 0).ShouldBeTrue();
    }

    private sealed class FakeDatabaseInitialization : IDatabaseInitialization
    {
        public Task Ready { get; set; } = Task.CompletedTask;

        public Task ReadyAsync() => Ready;
    }

    private sealed class FakeRootDirectoryCheck : IRootDirectoryCheck
    {
        public bool Exists { get; set; } = true;

        public Func<bool> Check { get; set; } = () => true;

        public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Exists && Check());
    }
}
