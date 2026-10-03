using AStarDev.WallpaperScraper.Scoping;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Operations;
using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.TestsUnit.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.WallpaperScraper.TestsUnit.Scraping;

public sealed class GivenAScrapeService : IDisposable
{
    private readonly OperationCoordinator operationCoordinator = new();
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository;
    private readonly FakeSearchOrchestrator searchOrchestrator = new();
    private readonly CapturingProgress progress = new();
    private readonly CapturingLogger<ScrapeService> logger = new();
    private readonly ServiceProvider serviceProvider;
    private readonly ScrapeService service;

    public GivenAScrapeService()
    {
        repository = unitOfWork.Register<ScrapeConfigurationEntity, ScrapeConfigurationId>();
        serviceProvider = new ServiceCollection()
            .AddSingleton<IUnitOfWork>(unitOfWork)
            .AddSingleton<ISearchOrchestrator>(searchOrchestrator)
            .BuildServiceProvider();

        service = new(operationCoordinator, new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()), logger);
    }

    [Fact]
    public async Task when_a_configuration_exists_then_it_is_searched_with_the_progress_and_completion_is_reported()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration();
        repository.First = (Option<ScrapeConfigurationEntity>)configuration;

        await Run();

        progress.Messages.ShouldContain("Starting scrape operation.");
        progress.Messages.ShouldContain(message => message.StartsWith("Search completed in:"));
        progress.Messages.ShouldContain($"Searched configuration {configuration.Id.Value}.");
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_search_is_running_then_the_operation_is_marked_as_running()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)ScrapeConfigurationTestData.CreateConfiguration();
        var wasRunning = false;
        searchOrchestrator.OnSearch = () => wasRunning = operationCoordinator.IsOperationRunning;

        await Run();

        wasRunning.ShouldBeTrue();
    }

    [Fact]
    public async Task when_no_configuration_row_exists_then_the_failure_is_reported_once_and_the_operation_still_completes()
    {
        repository.First = Option<ScrapeConfigurationEntity>.None.Instance;

        await Should.NotThrowAsync(Run);

        progress.Messages.ShouldContain("Starting scrape operation.");
        progress.Messages.Count(message => message.StartsWith("The scrape failed: ", StringComparison.Ordinal)).ShouldBe(1);
        progress.Messages.ShouldNotContain(message => message.StartsWith("Searched configuration", StringComparison.Ordinal));
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_looking_up_the_configuration_fails_then_the_failure_is_reported_once_and_the_operation_still_completes()
    {
        repository.First = new InvalidOperationException("query failed");

        await Should.NotThrowAsync(Run);

        progress.Messages.Count(message => message.Contains("query failed", StringComparison.Ordinal)).ShouldBe(1);
        progress.Messages.ShouldContain("The scrape failed: query failed");
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_searching_fails_then_the_whole_message_chain_is_reported_once()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)ScrapeConfigurationTestData.CreateConfiguration();
        searchOrchestrator.OnSearch = () => throw new InvalidOperationException("save failed", new InvalidOperationException("UNIQUE constraint failed"));

        await Should.NotThrowAsync(Run);

        progress.Messages.Where(message => message.Contains("failed", StringComparison.Ordinal) && !message.StartsWith("Searched", StringComparison.Ordinal)).ShouldBe(["The scrape failed: save failed Caused by: UNIQUE constraint failed"]);
    }

    [Fact]
    public async Task when_searching_raises_a_request_error_then_it_is_reported_once_not_thrown()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)ScrapeConfigurationTestData.CreateConfiguration();
        searchOrchestrator.OnSearch = () => throw new HttpRequestException("boom");

        await Run();

        progress.Messages.Where(message => message.Contains("boom", StringComparison.Ordinal)).ShouldBe(["The scrape failed: boom"]);
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_while_searching_then_cancellation_is_reported_not_thrown()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)ScrapeConfigurationTestData.CreateConfiguration();
        searchOrchestrator.OnSearch = () =>
        {
            operationCoordinator.Cancel();

            throw new OperationCanceledException();
        };

        await Run();

        progress.Messages.ShouldContain("Search cancelled.");
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_scrape_completes_then_its_start_and_end_are_logged()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)ScrapeConfigurationTestData.CreateConfiguration();

        await Run();

        (logger.Entries.Count, logger.Entries[0], logger.Entries[1].Level, logger.Entries[1].Message.StartsWith("Scrape completed in ", StringComparison.Ordinal)).ShouldBe((2, (LogLevel.Information, "Scrape started."), LogLevel.Information, true));
    }

    [Fact]
    public async Task when_a_scrape_is_cancelled_then_the_cancellation_is_logged()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)ScrapeConfigurationTestData.CreateConfiguration();
        searchOrchestrator.OnSearch = () =>
        {
            operationCoordinator.Cancel();

            throw new OperationCanceledException();
        };

        await Run();

        logger.Entries.ShouldBe([(LogLevel.Information, "Scrape started."), (LogLevel.Information, "Scrape cancelled.")]);
    }

    [Fact]
    public async Task when_a_scrape_fails_with_a_request_error_then_the_failure_is_logged_as_an_error()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)ScrapeConfigurationTestData.CreateConfiguration();
        searchOrchestrator.OnSearch = () => throw new HttpRequestException("boom");

        await Run();

        logger.Entries.ShouldBe([(LogLevel.Information, "Scrape started."), (LogLevel.Error, "Error occurred : `Scrape failed`")]);
    }

    [Fact]
    public async Task when_a_scrape_fails_unexpectedly_then_the_failure_is_logged_as_an_error()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)ScrapeConfigurationTestData.CreateConfiguration();
        searchOrchestrator.OnSearch = () => throw new InvalidOperationException("boom");

        await Run();

        logger.Entries.ShouldBe([(LogLevel.Information, "Scrape started."), (LogLevel.Error, "Error occurred : `Scrape failed`")]);
    }

    [Fact]
    public async Task when_an_operation_is_already_running_then_a_second_call_reports_it_and_leaves_the_running_operation_alone()
    {
        _ = operationCoordinator.TryStart(out _);

        await Run();

        progress.Messages.ShouldBe(["An operation is already running."]);
        operationCoordinator.IsOperationRunning.ShouldBeTrue();
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
        operationCoordinator.Dispose();
    }

    private Task Run() => service.RunScraperAsync(ScrapeSelection.All, progress);

    private sealed class FakeSearchOrchestrator : ISearchOrchestrator
    {
        public Action OnSearch { get; set; } = () => { };

        public Task RunSearchesAsync(ScrapeConfigurationEntity configuration, ScrapeSelection selection, IProgress<string> progress, CancellationToken cancellationToken)
        {
            progress.Report($"Searched configuration {configuration.Id.Value}.");
            OnSearch();

            return Task.CompletedTask;
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
