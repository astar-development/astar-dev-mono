using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.WallpaperScraper.TestsUnit.Fakes;
using AStarDev.WallpaperScraper.WallpaperIngestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.WallpaperScraper.TestsUnit.Scraping;

/// <summary>Runs the real <see cref="PagesProcessor"/> behind the orchestrator, faking only the network, directory and ingestion edges, and asserts on what was scraped and recorded.</summary>
public sealed class GivenASearchOrchestrator
{
    private readonly FakePageFetcher pageFetcher = new();
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly CapturingProgress progress = new();

    public GivenASearchOrchestrator() => _ = unitOfWork.Register<FileEntity, FileId>();

    [Fact]
    public async Task when_a_configuration_has_many_categories_then_hot_wallpapers_then_top_wallpapers_then_up_to_three_categories_are_processed()
    {
        await Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 5));

        progress.Messages.Where(message => message.StartsWith("Fetching ", StringComparison.Ordinal) && !message.Contains(" page ", StringComparison.Ordinal)).ShouldBe(["Fetching hot wallpapers.", "Fetching top wallpapers.", "Fetching categories."]);
        FetchedLabels.ShouldBe(["hot wallpapers", "top wallpapers", "search category category one", "search category category two", "search category category three"]);
        IngestedLabels.ShouldBe(["Hot Wallpapers", "Top Wallpapers", "category one", "category two", "category three"]);
    }

    [Theory]
    [InlineData(false, true, true, new[] { "top wallpapers", "search category category one", "search category category two" })]
    [InlineData(true, false, true, new[] { "hot wallpapers", "search category category one", "search category category two" })]
    [InlineData(true, true, false, new[] { "hot wallpapers", "top wallpapers" })]
    [InlineData(false, false, false, new string[0])]
    public async Task when_a_scrape_is_switched_off_then_only_the_scrapes_still_switched_on_are_processed(bool hot, bool top, bool categories, string[] expected)
    {
        await Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 2), new ScrapeSelection(hot, top, categories));

        FetchedLabels.ShouldBe(expected);
    }

    [Fact]
    public async Task when_a_category_is_excluded_from_the_search_then_it_is_not_processed_and_the_limit_applies_to_the_included_categories()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 5);
        configuration.SearchConfiguration.SearchCategories.First().IncludeInSearch = false;

        await Run(configuration);

        FetchedLabels.ShouldBe(["hot wallpapers", "top wallpapers", "search category category two", "search category category three", "search category category four"]);
    }

    [Fact]
    public async Task when_the_default_limits_are_used_then_every_category_is_processed()
    {
        await Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 5), ScrapeLimits.Default);

        FetchedLabels.Count().ShouldBe(7);
    }

    [Fact]
    public async Task when_the_category_limit_is_lower_then_only_that_many_categories_are_processed()
    {
        await Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 5), new ScrapeLimits(1, 4));

        FetchedLabels.ShouldBe(["hot wallpapers", "top wallpapers", "search category category one"]);
    }

    [Fact]
    public async Task when_pages_are_processed_then_every_page_is_fetched_with_the_configured_connection_and_ingested_with_the_person_categories()
    {
        await Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 2));

        pageFetcher.Fetches.ShouldAllBe(fetch => fetch.Contains("via https://example.test/ with key api-key", StringComparison.Ordinal));
        progress.Messages.Where(message => message.StartsWith("Ingested ", StringComparison.Ordinal)).ShouldAllBe(message => message.EndsWith("with people Celebrities,Models.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_a_configuration_has_no_categories_then_only_the_hot_and_top_wallpapers_are_processed()
    {
        await Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 0));

        FetchedLabels.ShouldBe(["hot wallpapers", "top wallpapers"]);
    }

    [Fact]
    public async Task when_the_hot_wallpapers_search_fails_then_the_failure_is_reported_once_and_the_later_searches_still_run()
    {
        pageFetcher.FailWhen = (label, _) => label == "hot wallpapers";

        await Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 2));

        FetchedLabels.ShouldBe(["top wallpapers", "search category category one", "search category category two"]);
        progress.Messages.Where(message => message.Contains("boom", StringComparison.Ordinal)).ShouldHaveSingleItem().ShouldBe("The hot wallpapers search failed: boom");
    }

    [Fact]
    public async Task when_a_category_search_fails_then_the_next_category_is_still_processed_and_the_failure_is_reported_once()
    {
        pageFetcher.FailWhen = (label, _) => label == "search category category one";

        await Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 3));

        FetchedLabels.ShouldBe(["hot wallpapers", "top wallpapers", "search category category two", "search category category three"]);
        progress.Messages.Where(message => message.Contains("boom", StringComparison.Ordinal)).ShouldHaveSingleItem().ShouldBe("The search category category one search failed: boom");
    }

    [Fact]
    public async Task when_the_scrape_is_cancelled_then_the_cancellation_propagates_and_later_searches_are_not_run()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => RunWithToken(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 2), new ScrapeLimits(3, 4), cancellationTokenSource.Token));

        progress.Messages.ShouldNotContain("Fetching top wallpapers.");
        progress.Messages.ShouldNotContain(message => message.Contains("search failed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_saving_a_page_fails_then_the_failure_propagates_and_later_searches_are_not_run()
    {
        unitOfWork.OnSave = _ => throw new DbUpdateException("save failed");

        _ = await Should.ThrowAsync<DbUpdateException>(() => Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 2)));

        progress.Messages.ShouldNotContain("Fetching top wallpapers.");
    }

    [Fact]
    public async Task when_a_category_is_scraped_then_its_progress_is_recorded_and_saved_after_its_page()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 1);
        pageFetcher.Meta = new Meta(1, 50);

        await Run(configuration);

        var category = configuration.SearchConfiguration.SearchCategories.Single();
        (category.LastKnownImageCount, category.LastPageVisited, category.TotalPages, unitOfWork.SaveCount).ShouldBe((50, 1, 1, 3));
    }

    [Fact]
    public async Task when_a_category_is_unchanged_since_its_previous_scrape_then_it_is_skipped_with_its_progress_unchanged_and_nothing_saved_for_it()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 1);
        configuration.SearchConfiguration.SearchCategories.Single().RecordScrapeProgress(50, 1, 1);
        pageFetcher.Meta = new Meta(1, 50);

        await Run(configuration);

        var category = configuration.SearchConfiguration.SearchCategories.Single();
        (category.LastKnownImageCount, category.LastPageVisited, category.TotalPages, unitOfWork.SaveCount).ShouldBe((50, 1, 1, 2));
        progress.Messages.ShouldContain("Skipping search category category one - nothing has changed since the last scrape.");
    }

    [Fact]
    public async Task when_a_category_stopped_part_way_last_time_then_it_resumes_after_the_last_page_visited_and_hot_and_top_wallpapers_start_at_page_1()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 1);
        configuration.SearchConfiguration.SearchCategories.Single().RecordScrapeProgress(7, 3, 9);
        pageFetcher.Meta = new Meta(9, 7);

        await Run(configuration);

        pageFetcher.Fetches.ShouldContain(fetch => fetch.StartsWith("Fetched hot wallpapers page 1 ", StringComparison.Ordinal));
        pageFetcher.Fetches.ShouldContain(fetch => fetch.StartsWith("Fetched top wallpapers page 1 ", StringComparison.Ordinal));
        pageFetcher.Fetches.ShouldContain(fetch => fetch.StartsWith("Fetched search category category one page 4 ", StringComparison.Ordinal));
        pageFetcher.Fetches.ShouldNotContain(fetch => fetch.StartsWith("Fetched search category category one page 1 ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_a_category_page_completes_and_a_later_page_fails_then_the_progress_so_far_is_recorded_on_the_category()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 1);
        pageFetcher.Meta = new Meta(3, 50);
        pageFetcher.FailWhen = (label, page) => label == "search category category one" && page == 2;

        await Run(configuration);

        var category = configuration.SearchConfiguration.SearchCategories.Single();
        (category.LastKnownImageCount, category.LastPageVisited, category.TotalPages).ShouldBe((50, 1, 3));
        progress.Messages.ShouldContain("The search category category one search failed: boom");
    }

    [Fact]
    public async Task when_hot_and_top_wallpaper_pages_complete_then_no_category_progress_is_saved()
    {
        await Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 0));

        unitOfWork.SaveCount.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_hot_wallpapers_are_processed_then_the_first_page_url_uses_the_hot_wallpapers_setting_and_the_top_url_uses_the_top_setting()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 0);
        configuration.HotWallpapers = "hot/";

        await Run(configuration);

        pageFetcher.Fetches.Select(fetch => fetch[(fetch.IndexOf(" from ", StringComparison.Ordinal) + 6)..fetch.IndexOf(" via ", StringComparison.Ordinal)]).ShouldBe(["hot/1", "top/1"]);
    }

    private IEnumerable<string> FetchedLabels => pageFetcher.Fetches.Select(fetch => fetch["Fetched ".Length..fetch.IndexOf(" page ", StringComparison.Ordinal)]);

    private IEnumerable<string> IngestedLabels => progress.Messages.Where(message => message.StartsWith("Ingested ", StringComparison.Ordinal)).Select(message => message["Ingested ".Length..message.IndexOf(" with people ", StringComparison.Ordinal)]);

    private Task Run(ScrapeConfigurationEntity configuration) => Run(configuration, new ScrapeLimits(3, 4));

    private Task Run(ScrapeConfigurationEntity configuration, ScrapeSelection selection) => RunWithToken(configuration, new ScrapeLimits(3, 4), selection, CancellationToken.None);

    private Task Run(ScrapeConfigurationEntity configuration, ScrapeLimits limits) => RunWithToken(configuration, limits, CancellationToken.None);

    private Task RunWithToken(ScrapeConfigurationEntity configuration, ScrapeLimits limits, CancellationToken cancellationToken)
        => RunWithToken(configuration, limits, ScrapeSelection.All, cancellationToken);

    private Task RunWithToken(ScrapeConfigurationEntity configuration, ScrapeLimits limits, ScrapeSelection selection, CancellationToken cancellationToken)
    {
        var pagesProcessor = new PagesProcessor(new WallpaperIngestionContextFactory(new FakeClientFactory(), unitOfWork, new FakeSaveDirectoryResolver()), pageFetcher, new PageIngestionStep(new FakeIngestionService(), unitOfWork), new ScrapeResumePolicy(limits), DownloadPacing.None, TimeProvider.System);

        return new SearchOrchestrator(pagesProcessor, limits, NullLogger<SearchOrchestrator>.Instance).RunSearchesAsync(configuration, selection, progress, cancellationToken);
    }

    private sealed class FakeClientFactory : IWallhavenClientFactory
    {
        public HttpClient Create(WallhavenConnection connection)
        {
            var client = new HttpClient { BaseAddress = connection.BaseUrl };
            client.DefaultRequestHeaders.Add("X-API-Key", connection.ApiKey);

            return client;
        }
    }

    private sealed class FakePageFetcher : IWallhavenPageFetcher
    {
        public List<string> Fetches { get; } = [];

        public Meta Meta { get; set; } = new(1);

        public Func<string, int, bool> FailWhen { get; set; } = (_, _) => false;

        public Task<SearchResponse> FetchPageAsync(PageFetchRequest request, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FailWhen(request.LogLabel, request.Page)) throw new HttpRequestException("boom");

            Fetches.Add($"Fetched {request.LogLabel} page {request.Page} from {request.PageUrl} via {client.BaseAddress} with key {client.DefaultRequestHeaders.GetValues("X-API-Key").Single()}");

            return Task.FromResult(new SearchResponse([new Data("wallpaper", 0, 0, 0, "", "")], Meta));
        }
    }

    private sealed class FakeSaveDirectoryResolver : ISaveDirectoryResolver
    {
        public Task<SaveDirectories> ResolveSaveDirectoriesAsync(Option<string> categoryName, CancellationToken cancellationToken) => Task.FromResult(new SaveDirectories("root", "famous", "segment"));
    }

    private sealed class FakeIngestionService : IWallpaperIngestionService
    {
        public Task<IngestOutcome> IngestPageAsync(IReadOnlyList<Data> wallpapers, IngestionRun run)
        {
            run.Progress.Report($"Ingested {run.Context.Output.CategoryLabel} with people {string.Join(",", run.Context.PersonCategories)}.");

            return Task.FromResult(IngestOutcome.Complete);
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
