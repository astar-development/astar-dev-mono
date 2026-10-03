using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.WallpaperScraper.TestsUnit.Fakes;
using AStarDev.WallpaperScraper.WallpaperIngestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.WallpaperScraper.TestsUnit.Scraping;

public sealed class GivenAPagesProcessor
{
    private static readonly string[] personCategories = ["Celebrities"];
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeJsonResponseProcessor jsonResponseProcessor = new();
    private readonly FakePageFetcher pageFetcher;
    private readonly FakeIngestionService wallpaperIngestionService = new();
    private readonly CapturingProgress progress = new();
    private readonly List<SearchCategoryProgress> completedProgress = [];
    private readonly PagesProcessor processor;

    public GivenAPagesProcessor()
    {
        _ = unitOfWork.Register<FileEntity, FileId>();
        pageFetcher = new(jsonResponseProcessor);
        processor = CreateProcessor(new ScrapeLimits(3, 4));
    }

    [Fact]
    public async Task when_a_page_is_fetched_then_all_of_its_wallpapers_are_ingested_in_a_single_call()
    {
        SetUpPage(1, CreateSearchResponse(lastPage: 1, CreateWallpaper("wallpaper-1"), CreateWallpaper("wallpaper-2"), CreateWallpaper("wallpaper-3")));

        await Run();

        IngestedPages.ShouldBe(["Ingested page of 3."]);
    }

    [Fact]
    public async Task when_a_page_is_fetched_then_each_wallpaper_on_it_is_ingested_into_the_resolved_directory()
    {
        var wallpaper = CreateWallpaper("wallpaper-1");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));

        await Run();

        progress.Messages.ShouldContain("Ingested wallpaper-1 into resolved-directory|resolved-famous-directory|resolved-category-segment as Top Wallpapers with people Celebrities.");
        unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_specific_search_category_is_supplied_then_wallpapers_are_ingested_with_that_category_as_the_label()
    {
        var wallpaper = CreateWallpaper("wallpaper-category");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));

        await processor.FetchAndProcessPagesAsync(CreateRequest("search category Cars", Option.Some("Cars"), Option.None<SearchCategoryProgress>(), _ => { }), progress, CancellationToken.None);

        progress.Messages.ShouldContain(message => message.StartsWith("Ingested wallpaper-category ", StringComparison.Ordinal) && message.Contains(" as Cars ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_ingesting_a_wallpaper_fails_then_the_failure_is_rethrown_unreported()
    {
        var wallpaper = CreateWallpaper("wallpaper-2");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));
        wallpaperIngestionService.OnIngest = () => throw new InvalidOperationException("ingestion failed");

        await Should.ThrowAsync<InvalidOperationException>(Run);

        progress.Messages.ShouldNotContain(message => message.Contains("ingestion failed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_the_reported_last_page_is_reached_before_the_hard_cap_then_paging_stops_there()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 2));

        await Run();

        progress.Messages.ShouldContain("Fetching wallpapers page 1.");
        progress.Messages.ShouldContain("Fetching wallpapers page 2.");
        progress.Messages.ShouldNotContain(message => message.Contains("page 3"));
        unitOfWork.SaveCount.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_reported_last_page_exceeds_the_hard_cap_then_paging_stops_at_page_4()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 10));

        await Run();

        progress.Messages.ShouldContain("Fetching wallpapers page 4.");
        progress.Messages.ShouldNotContain(message => message.Contains("page 5"));
        unitOfWork.SaveCount.ShouldBe(4);
    }

    [Fact]
    public async Task when_the_scrape_resumes_part_way_then_the_wallpapers_on_the_pages_resumed_past_count_as_skipped()
    {
        SetUpPage(3, CreateSearchResponse(lastPage: 3, total: 6, CreateWallpaper("a"), CreateWallpaper("b")));

        await Fetch(Option.Some(new SearchCategoryProgress(6, 2, 3)), CancellationToken.None);

        wallpaperIngestionService.CountsSeen.ShouldBe([4]);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2025:Ensure tasks using 'IDisposable' instances complete before the instances are disposed", Justification = "The fetch is awaited before the test ends; the clock must advance while it is pending.")]
    public async Task when_there_is_another_page_then_the_pacing_delay_passes_before_it_is_fetched()
    {
        var clock = new FakeTimeProvider();
        var pacedProcessor = new PagesProcessor(new WallpaperIngestionContextFactory(new FakeClientFactory(), unitOfWork, new FakeSaveDirectoryResolver()), pageFetcher, new PageIngestionStep(wallpaperIngestionService, unitOfWork), new ScrapeResumePolicy(new ScrapeLimits(3, 4)), new DownloadPacing(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)), clock);
        SetUpPage(page: null, CreateSearchResponse(lastPage: 2));

        var fetch = pacedProcessor.FetchAndProcessPagesAsync(CreateRequest("wallpapers", Option.None<string>(), Option.None<SearchCategoryProgress>(), _ => { }), progress, TestContext.Current.CancellationToken);
        await Task.Yield();
        clock.Advance(TimeSpan.FromSeconds(1.9));
        var fetchedBeforeTheDelay = progress.Messages.Count(message => message.StartsWith("Fetched ", StringComparison.Ordinal) || message.Contains("page 2", StringComparison.Ordinal));
        clock.Advance(TimeSpan.FromSeconds(0.2));
        await fetch;

        (fetchedBeforeTheDelay, progress.Messages.Count(message => message.Contains("page 2", StringComparison.Ordinal))).ShouldBe((0, 1));
    }

    [Fact]
    public async Task when_the_page_limit_is_lower_than_the_reported_last_page_then_paging_stops_at_the_limit()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 10));
        var limitedProcessor = CreateProcessor(new ScrapeLimits(3, 2));

        await limitedProcessor.FetchAndProcessPagesAsync(CreateRequest("wallpapers", Option.None<string>(), Option.None<SearchCategoryProgress>(), _ => { }), progress, CancellationToken.None);

        (unitOfWork.SaveCount, progress.Messages.Any(message => message.Contains("page 3", StringComparison.Ordinal))).ShouldBe((2, false));
    }

    [Fact]
    public async Task when_every_page_is_processed_then_the_last_page_reports_the_observed_progress()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 2, total: 50));

        await Fetch(Option.None<SearchCategoryProgress>(), CancellationToken.None);

        completedProgress.Last().ShouldBe(new SearchCategoryProgress(50, 2, 2));
    }

    [Fact]
    public async Task when_paging_stops_at_the_limit_then_the_last_page_visited_reported_is_the_limit()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 10, total: 100));

        await Fetch(Option.None<SearchCategoryProgress>(), CancellationToken.None);

        completedProgress.Last().ShouldBe(new SearchCategoryProgress(100, 4, 10));
    }

    [Fact]
    public async Task when_the_count_and_total_pages_match_the_previous_scrape_then_nothing_is_ingested_and_no_progress_is_reported()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 2, total: 50, CreateWallpaper("wallpaper-1")));

        await Fetch(Option.Some(new SearchCategoryProgress(50, 2, 2)), CancellationToken.None);

        completedProgress.ShouldBeEmpty();
        unitOfWork.SaveCount.ShouldBe(0);
        IngestedPages.ShouldBeEmpty();
        progress.Messages.ShouldContain("Skipping wallpapers - nothing has changed since the last scrape.");
    }

    [Theory]
    [InlineData(49, 2, 2)]
    [InlineData(50, 2, 3)]
    [InlineData(0, 0, 0)]
    public async Task when_the_previous_scrape_differs_then_the_pages_are_ingested(int previousCount, int previousPageVisited, int previousTotalPages)
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 2, total: 50, CreateWallpaper("wallpaper-1")));

        await Fetch(Option.Some(new SearchCategoryProgress(previousCount, previousPageVisited, previousTotalPages)), CancellationToken.None);

        completedProgress.Last().ShouldBe(new SearchCategoryProgress(50, 2, 2));
        IngestedPages.ShouldBe(["Ingested page of 1.", "Ingested page of 1."]);
    }

    [Fact]
    public async Task when_a_page_is_ingested_then_the_progress_so_far_is_reported_before_the_page_is_saved()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 2, total: 50));

        await Fetch(Option.None<SearchCategoryProgress>(), CancellationToken.None);

        completedProgress.ShouldBe([new SearchCategoryProgress(50, 1, 2), new SearchCategoryProgress(50, 2, 2)]);
    }

    [Fact]
    public async Task when_a_later_page_has_a_wallpaper_that_was_not_ingested_then_progress_stays_at_the_last_fully_ingested_page_so_it_is_retried()
    {
        SetUpPage(1, CreateSearchResponse(lastPage: 2, total: 50, CreateWallpaper("page-1-wallpaper")));
        SetUpPage(2, CreateSearchResponse(lastPage: 2, total: 50, CreateWallpaper("page-2-wallpaper")));
        wallpaperIngestionService.IncompletePages.Add("page-2-wallpaper");

        await Fetch(Option.Some(new SearchCategoryProgress(0, 0, 0)), CancellationToken.None);

        completedProgress.ShouldBe([new SearchCategoryProgress(50, 1, 2)]);
        unitOfWork.SaveCount.ShouldBe(2);
        progress.Messages.ShouldContain("Not every wallpaper on page 2 was ingested - progress stays before that page so it is retried on the next scrape.");
    }

    [Fact]
    public async Task when_there_is_no_previous_progress_and_a_page_has_a_wallpaper_that_was_not_ingested_then_no_progress_withheld_message_is_reported()
    {
        SetUpPage(1, CreateSearchResponse(lastPage: 2, total: 50, CreateWallpaper("page-1-wallpaper")));
        SetUpPage(2, CreateSearchResponse(lastPage: 2, total: 50, CreateWallpaper("page-2-wallpaper")));
        wallpaperIngestionService.IncompletePages.Add("page-2-wallpaper");

        await Fetch(Option.None<SearchCategoryProgress>(), CancellationToken.None);

        progress.Messages.ShouldNotContain(message => message.StartsWith("Not every wallpaper", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_the_first_page_has_a_wallpaper_that_was_not_ingested_then_no_page_reports_progress_even_if_later_pages_are_fully_ingested()
    {
        SetUpPage(1, CreateSearchResponse(lastPage: 2, total: 50, CreateWallpaper("page-1-wallpaper")));
        SetUpPage(2, CreateSearchResponse(lastPage: 2, total: 50, CreateWallpaper("page-2-wallpaper")));
        wallpaperIngestionService.IncompletePages.Add("page-1-wallpaper");

        await Fetch(Option.None<SearchCategoryProgress>(), CancellationToken.None);

        completedProgress.ShouldBeEmpty();
        IngestedPages.Count().ShouldBe(2);
    }

    [Fact]
    public async Task when_the_previous_scrape_stopped_part_way_then_paging_resumes_after_the_last_page_visited()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 3, total: 50, CreateWallpaper("wallpaper-1")));

        await Fetch(Option.Some(new SearchCategoryProgress(50, 1, 3)), CancellationToken.None);

        completedProgress.Last().ShouldBe(new SearchCategoryProgress(50, 3, 3));
        progress.Messages.ShouldNotContain("Fetching wallpapers page 1.");
        progress.Messages.ShouldContain("Fetching wallpapers page 2.");
        IngestedPages.ShouldBe(["Ingested page of 1.", "Ingested page of 1."]);
    }

    [Fact]
    public async Task when_the_previous_scrape_stopped_part_way_and_the_count_has_changed_then_paging_restarts_from_page_1()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 3, total: 60, CreateWallpaper("wallpaper-1")));

        await Fetch(Option.Some(new SearchCategoryProgress(50, 2, 3)), CancellationToken.None);

        completedProgress.Last().ShouldBe(new SearchCategoryProgress(60, 3, 3));
        progress.Messages.ShouldContain("Fetching wallpapers page 1.");
        IngestedPages.ShouldBe(["Ingested page of 1.", "Ingested page of 1.", "Ingested page of 1."]);
    }

    [Fact]
    public async Task when_the_previous_scrape_stopped_part_way_and_the_total_pages_have_changed_then_paging_restarts_from_page_1()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 2, total: 50, CreateWallpaper("wallpaper-1")));

        await Fetch(Option.Some(new SearchCategoryProgress(50, 1, 3)), CancellationToken.None);

        progress.Messages.ShouldContain("Fetching wallpapers page 1.");
    }

    [Fact]
    public async Task when_the_previous_scrape_visited_every_page_up_to_the_limit_then_the_category_is_skipped()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 10, total: 100));

        await Fetch(Option.Some(new SearchCategoryProgress(100, 4, 10)), CancellationToken.None);

        completedProgress.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_scrape_is_cancelled_before_any_page_is_ingested_then_nothing_is_saved_and_no_saved_message_is_reported()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        pageFetcher.OnFetch = () =>
        {
            cancellationTokenSource.Cancel();

            throw new OperationCanceledException(cancellationTokenSource.Token);
        };

        await Should.ThrowAsync<OperationCanceledException>(() => FetchWithCancellation(cancellationTokenSource.Token));

        unitOfWork.SaveCount.ShouldBe(0);
        progress.Messages.ShouldNotContain(message => message.StartsWith("Scrape cancelled", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_the_scrape_is_cancelled_while_fetching_a_later_page_then_nothing_more_is_saved_and_no_saved_message_is_reported()
    {
        SetUpPage(1, CreateSearchResponse(lastPage: 2, total: 50, CreateWallpaper("page-1-wallpaper")));
        using var cancellationTokenSource = new CancellationTokenSource();
        pageFetcher.OnFetch = () =>
        {
            if (pageFetcher.FetchCount < 2) return;

            cancellationTokenSource.Cancel();

            throw new OperationCanceledException(cancellationTokenSource.Token);
        };

        await Should.ThrowAsync<OperationCanceledException>(() => FetchWithCancellation(cancellationTokenSource.Token));

        unitOfWork.SaveCount.ShouldBe(1);
        progress.Messages.ShouldNotContain(message => message.StartsWith("Scrape cancelled", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_ingesting_a_wallpaper_is_cancelled_mid_page_then_the_partial_page_is_saved_before_the_cancellation_propagates()
    {
        var wallpaper = CreateWallpaper("wallpaper-cancelled");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));
        using var cancellationTokenSource = new CancellationTokenSource();
        wallpaperIngestionService.OnIngest = () =>
        {
            cancellationTokenSource.Cancel();

            throw new OperationCanceledException(cancellationTokenSource.Token);
        };

        await Should.ThrowAsync<OperationCanceledException>(() => FetchWithCancellation(cancellationTokenSource.Token));

        unitOfWork.SaveTokens.ShouldBe([CancellationToken.None]);
        progress.Messages.ShouldContain("Scrape cancelled - saved wallpapers downloaded so far this page.");
    }

    [Fact]
    public async Task when_the_partial_page_save_on_cancellation_itself_fails_then_the_cancellation_still_propagates()
    {
        var wallpaper = CreateWallpaper("wallpaper-cancelled-save-fails");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));
        using var cancellationTokenSource = new CancellationTokenSource();
        wallpaperIngestionService.OnIngest = () =>
        {
            cancellationTokenSource.Cancel();

            throw new OperationCanceledException(cancellationTokenSource.Token);
        };
        unitOfWork.OnSave = token =>
        {
            if (token == CancellationToken.None) throw new DbUpdateException("save failed", new InvalidOperationException("UNIQUE constraint failed: FileDetail.FileHandle"));
        };

        await Should.ThrowAsync<OperationCanceledException>(() => FetchWithCancellation(cancellationTokenSource.Token));

        progress.Messages.ShouldContain("Scrape cancelled - failed to save wallpapers downloaded so far this page: save failed Caused by: UNIQUE constraint failed: FileDetail.FileHandle");
    }

    [Fact]
    public async Task when_saving_a_page_fails_with_a_database_update_error_then_the_failure_is_rethrown_for_the_scrape_service_to_report()
    {
        SetUpPage(1, CreateSearchResponse(lastPage: 1, CreateWallpaper("wallpaper-duplicate")));
        unitOfWork.OnSave = _ => throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", new InvalidOperationException("UNIQUE constraint failed: FileDetail.FileHandle"));

        _ = await Should.ThrowAsync<DbUpdateException>(Run);

        progress.Messages.ShouldNotContain(message => message.Contains("UNIQUE constraint failed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_fetching_a_page_fails_then_the_failure_is_rethrown_as_a_page_fetch_failure_unreported()
    {
        var exception = new InvalidOperationException("page fetch failed");
        jsonResponseProcessor.Response = exception;

        var thrown = await Should.ThrowAsync<PageFetchException>(Run);

        thrown.Failure.ShouldBeSameAs(exception);
        progress.Messages.ShouldNotContain(message => message.Contains("page fetch failed", StringComparison.Ordinal));
    }

    private PagesProcessor CreateProcessor(ScrapeLimits limits)
        => new(new WallpaperIngestionContextFactory(new FakeClientFactory(), unitOfWork, new FakeSaveDirectoryResolver()), pageFetcher, new PageIngestionStep(wallpaperIngestionService, unitOfWork), new ScrapeResumePolicy(limits), DownloadPacing.None, TimeProvider.System);

    private Task FetchWithCancellation(CancellationToken cancellationToken)
        => Fetch(Option.None<SearchCategoryProgress>(), cancellationToken);

    private Task Fetch(Option<SearchCategoryProgress> previousProgress, CancellationToken cancellationToken)
        => processor.FetchAndProcessPagesAsync(CreateRequest("wallpapers", Option.None<string>(), previousProgress, completedProgress.Add), progress, cancellationToken);

    private static PageScrapeRequest CreateRequest(string logLabel, Option<string> categoryName, Option<SearchCategoryProgress> previousProgress, Action<SearchCategoryProgress> onPageCompleted)
        => new(new ScrapeLabel(logLabel, categoryName), previousProgress, new PageHooks(onPageCompleted, page => new Uri($"https://example.test/page/{page}")), new ScrapeTarget(new WallhavenConnection("api-key", new Uri("https://example.test")), personCategories));

    private IEnumerable<string> IngestedPages => progress.Messages.Where(message => message.StartsWith("Ingested page of ", StringComparison.Ordinal));

    private Task Run()
        => FetchWithCancellation(CancellationToken.None);

    private void SetUpPage(int? page, SearchResponse response)
    {
        if (page is { } specificPage)
        {
            jsonResponseProcessor.PageResponses[new Uri($"https://example.test/page/{specificPage}")] = response;

            return;
        }

        jsonResponseProcessor.Response = (Option<SearchResponse>)response;
    }

    private static SearchResponse CreateSearchResponse(int lastPage, params Data[] wallpapers)
        => new(wallpapers, new Meta(lastPage));

    private static SearchResponse CreateSearchResponse(int lastPage, int total, params Data[] wallpapers)
        => new(wallpapers, new Meta(lastPage, total));

    private static Data CreateWallpaper(string id, string path = "")
        => new(id, 0, 0, 0, "", path);

    private sealed class FakeClientFactory : IWallhavenClientFactory
    {
        public HttpClient Create(WallhavenConnection connection) => new();
    }

    private sealed class FakePageFetcher(FakeJsonResponseProcessor jsonResponseProcessor) : IWallhavenPageFetcher
    {
        public Action OnFetch { get; set; } = () => { };

        public int FetchCount { get; private set; }

        public Task<SearchResponse> FetchPageAsync(PageFetchRequest request, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
        {
            FetchCount++;
            progress.Report($"Fetching {request.LogLabel} page {request.Page}.");
            OnFetch();

            return Task.FromResult(jsonResponseProcessor.Fetch(request.PageUrl));
        }
    }

    private sealed class FakeSaveDirectoryResolver : ISaveDirectoryResolver
    {
        public Task<SaveDirectories> ResolveSaveDirectoriesAsync(Option<string> categoryName, CancellationToken cancellationToken) => Task.FromResult(new SaveDirectories("resolved-directory", "resolved-famous-directory", "resolved-category-segment"));
    }

    private sealed class FakeJsonResponseProcessor : IJsonResponseProcessor
    {
        public Dictionary<Uri, SearchResponse> PageResponses { get; } = [];

        public Exceptional<Option<SearchResponse>> Response { get; set; } = Option<SearchResponse>.None.Instance;

        public Task<Exceptional<Option<T>>> GetFromJsonAsync<T>(Uri url, HttpClient client, CancellationToken cancellationToken)
        {
            Exceptional<Option<SearchResponse>> result = PageResponses.TryGetValue(url, out var page) ? (Option<SearchResponse>)page : Response;

            return Task.FromResult((Exceptional<Option<T>>)(object)result);
        }

        public SearchResponse Fetch(Uri url)
        {
            Exceptional<Option<SearchResponse>> result = PageResponses.TryGetValue(url, out var page) ? (Option<SearchResponse>)page : Response;

            return result.Match(
                option => option.Match(value => value, () => throw new InvalidOperationException($"No response body received for {url}.")),
                exception => throw exception);
        }
    }

    private sealed class FakeIngestionService : IWallpaperIngestionService
    {
        public Action OnIngest { get; set; } = () => { };

        /// <summary>The wallpapers the scrape had got through when each page was handed over for ingestion.</summary>
        public List<int> CountsSeen { get; } = [];

        /// <summary>The outcome for each page (by the first wallpaper's id) that should not be complete.</summary>
        public HashSet<string> IncompletePages { get; } = [];

        public Task<IngestOutcome> IngestPageAsync(IReadOnlyList<Data> wallpapers, IngestionRun run)
        {
            CountsSeen.Add(run.Tally.Current);
            var (context, progress) = (run.Context, run.Progress);
            progress.Report($"Ingested page of {wallpapers.Count}.");
            foreach (var wallpaper in wallpapers) progress.Report($"Ingested {wallpaper.Id} into {context.Output.Directories.Root}|{context.Output.Directories.FamousRoot}|{context.Output.Directories.CategorySegment} as {context.Output.CategoryLabel} with people {string.Join(",", context.PersonCategories)}.");

            OnIngest();

            return Task.FromResult(wallpapers.Count > 0 && IncompletePages.Contains(wallpapers[0].Id) ? IngestOutcome.Incomplete : IngestOutcome.Complete);
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
