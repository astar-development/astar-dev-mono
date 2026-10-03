using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAPageIngestionStep : IDisposable
{
    private readonly HttpClient client = new();
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly RecordingIngestionService ingestionService = new();
    private readonly List<string> events = [];
    private readonly List<string> messages = [];
    private readonly PageIngestionStep step;

    public GivenAPageIngestionStep()
    {
        _ = unitOfWork.Register<FileEntity, FileId>();
        unitOfWork.OnSave = _ => events.Add("save");
        ingestionService.OnIngest = () => events.Add("ingest");
        step = new(ingestionService, unitOfWork);
    }

    [Fact]
    public async Task when_a_page_is_ingested_then_the_wallpapers_are_ingested_the_progress_reported_and_the_page_saved_in_that_order()
    {
        await step.IngestPageAsync(CreateRun(progress => events.Add($"hook {progress.LastKnownImageCount}/{progress.LastPageVisited}/{progress.TotalPages}")), new FetchedPage(2, new SearchResponse([new Data("w", 0, 0, 0, "", "")], new Meta(5, 40))), recordProgress: true);

        events.ShouldBe(["ingest", "hook 40/2/5", "save"]);
    }

    [Fact]
    public async Task when_a_page_is_ingested_then_the_search_total_is_made_available_to_the_ingestion()
    {
        var run = CreateRun(_ => { });

        await step.IngestPageAsync(run, new FetchedPage(1, new SearchResponse([new Data("w", 0, 0, 0, "", "")], new Meta(47, 1124))), recordProgress: true);

        ingestionService.TotalSeen.ShouldBe(1124);
    }

    [Fact]
    public async Task when_a_wallpaper_on_the_page_was_not_ingested_then_no_progress_is_reported_but_the_page_is_still_saved_and_reported_incomplete()
    {
        ingestionService.Outcome = IngestOutcome.Incomplete;

        var outcome = await step.IngestPageAsync(CreateRun(progress => events.Add("hook")), new FetchedPage(2, new SearchResponse([new Data("w", 0, 0, 0, "", "")], new Meta(5, 40))), recordProgress: true);

        outcome.ShouldBe(IngestOutcome.Incomplete);
        events.ShouldBe(["ingest", "save"]);
    }

    [Fact]
    public async Task when_progress_is_being_withheld_then_a_fully_ingested_page_does_not_report_progress()
    {
        var outcome = await step.IngestPageAsync(CreateRun(progress => events.Add("hook")), new FetchedPage(3, new SearchResponse([new Data("w", 0, 0, 0, "", "")], new Meta(5, 40))), recordProgress: false);

        outcome.ShouldBe(IngestOutcome.Complete);
        events.ShouldBe(["ingest", "save"]);
    }

    [Fact]
    public async Task when_ingesting_is_cancelled_mid_page_then_the_partial_page_is_saved_ignoring_cancellation_and_the_cancellation_propagates()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        ingestionService.OnIngest = () => ThrowCancelled(cancellationTokenSource);

        await Should.ThrowAsync<OperationCanceledException>(() => step.IngestPageAsync(CreateCancellableRun(_ => { }, cancellationTokenSource.Token), CreatePage(), recordProgress: true));

        unitOfWork.SaveTokens.Single().CanBeCanceled.ShouldBeFalse();
        messages.ShouldBe(["Scrape cancelled - saved wallpapers downloaded so far this page."]);
    }

    [Fact]
    public async Task when_saving_partially_ingested_wallpapers_fails_then_the_failure_is_reported_and_the_cancellation_still_propagates()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        ingestionService.OnIngest = () => ThrowCancelled(cancellationTokenSource);
        unitOfWork.OnSave = _ => throw new DbUpdateException("save failed");

        await Should.ThrowAsync<OperationCanceledException>(() => step.IngestPageAsync(CreateCancellableRun(_ => { }, cancellationTokenSource.Token), CreatePage(), recordProgress: true));

        messages.ShouldBe(["Scrape cancelled - failed to save wallpapers downloaded so far this page: save failed"]);
    }

    [Fact]
    public async Task when_saving_partially_ingested_wallpapers_fails_for_any_other_reason_then_the_failure_is_reported_and_the_cancellation_still_propagates()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        ingestionService.OnIngest = () => ThrowCancelled(cancellationTokenSource);
        unitOfWork.OnSave = _ => throw new InvalidOperationException("context disposed");

        await Should.ThrowAsync<OperationCanceledException>(() => step.IngestPageAsync(CreateCancellableRun(_ => { }, cancellationTokenSource.Token), CreatePage(), recordProgress: true));

        messages.ShouldBe(["Scrape cancelled - failed to save wallpapers downloaded so far this page: context disposed"]);
    }

    [Fact]
    public async Task when_ingesting_fails_for_a_reason_other_than_cancellation_then_the_failure_propagates_without_a_partial_save()
    {
        ingestionService.OnIngest = () => throw new InvalidOperationException("ingestion failed");

        await Should.ThrowAsync<InvalidOperationException>(() => step.IngestPageAsync(CreateRun(_ => { }), CreatePage(), recordProgress: true));

        unitOfWork.SaveCount.ShouldBe(0);
        messages.ShouldBeEmpty();
    }

    public void Dispose() => client.Dispose();

    private static void ThrowCancelled(CancellationTokenSource cancellationTokenSource)
    {
        cancellationTokenSource.Cancel();

        throw new OperationCanceledException(cancellationTokenSource.Token);
    }

    private static FetchedPage CreatePage()
        => new(1, new SearchResponse([new Data("w", 0, 0, 0, "", "")], new Meta(5, 40)));

    private IngestionRun CreateRun(Action<SearchCategoryProgress> onPageCompleted)
        => CreateCancellableRun(onPageCompleted, CancellationToken.None);

    private IngestionRun CreateCancellableRun(Action<SearchCategoryProgress> onPageCompleted, CancellationToken cancellationToken)
        => new(
            new PageScrapeRequest(new ScrapeLabel("label", Option.None<string>()), Option.None<SearchCategoryProgress>(), new PageHooks(onPageCompleted, page => new Uri($"https://example.test/page/{page}")), new ScrapeTarget(new WallhavenConnection("api-key", new Uri("https://example.test")), [])),
            new WallpaperIngestionContext(new SearchOutput(new SaveDirectories("root", "famous", "segment"), "Top Wallpapers"), client, new FakeRepository<FileEntity, FileId>(), []),
            new Progress(messages),
            cancellationToken);

    private sealed class Progress(List<string> messages) : IProgress<string>
    {
        public void Report(string value) => messages.Add(value);
    }

    private sealed class RecordingIngestionService : IWallpaperIngestionService
    {
        public Action OnIngest { get; set; } = () => { };

        public IngestOutcome Outcome { get; set; } = IngestOutcome.Complete;

        public int TotalSeen { get; private set; }

        public Task<IngestOutcome> IngestPageAsync(IReadOnlyList<Data> wallpapers, IngestionRun run)
        {
            OnIngest();
            TotalSeen = run.Tally.Total;

            return Task.FromResult(Outcome);
        }
    }
}
