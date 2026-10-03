using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.UI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenAScrapeRunner
{
    private readonly FakeScrapeService scrapeService = new();
    private readonly StatusReporter status = TestStatusReporter.Create();
    private readonly ScrapeRunner runner;

    public GivenAScrapeRunner() => runner = new(scrapeService, status);

    [Fact]
    public async Task when_the_scrape_reports_progress_then_each_message_is_appended_to_the_status()
    {
        scrapeService.OnRun = progress =>
        {
            progress.Report("Starting scrape operation.");
            progress.Report("Fetching categories.");
        };

        await runner.RunAsync(ScrapeSelection.All);

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} Starting scrape operation.{Environment.NewLine}{TestStatusReporter.Timestamp} Fetching categories.");
    }

    [Fact]
    public async Task when_the_scrape_throws_then_the_failure_is_reported_and_not_thrown()
    {
        scrapeService.OnRun = _ => throw new InvalidOperationException("Scrape configuration not found");

        await Should.NotThrowAsync(() => runner.RunAsync(ScrapeSelection.All));

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} The scrape failed. Scrape configuration not found");
    }

    [Fact]
    public async Task when_run_from_a_thread_with_a_synchronisation_context_then_the_scrape_runs_without_it()
    {
        var originalContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

        try
        {
            var scrapeContext = new SynchronizationContext();
            scrapeService.OnRun = _ => scrapeContext = SynchronizationContext.Current!;

            await runner.RunAsync(ScrapeSelection.All);

            scrapeContext.ShouldBeNull();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    private sealed class FakeScrapeService : IScrapeService
    {
        public Action<IProgress<string>> OnRun { get; set; } = _ => { };

        public Task RunScraperAsync(ScrapeSelection selection, IProgress<string> progress)
        {
            OnRun(progress);

            return Task.CompletedTask;
        }
    }
}
