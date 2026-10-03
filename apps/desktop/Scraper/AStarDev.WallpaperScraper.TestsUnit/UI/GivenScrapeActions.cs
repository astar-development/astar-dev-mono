using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Downloads;
using AStarDev.WallpaperScraper.Operations;
using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.UI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.WallpaperScraper.TestsUnit.UI;

public sealed class GivenScrapeActions : IDisposable
{
    private readonly OperationCoordinator coordinator = new();
    private readonly StatusReporter status = TestStatusReporter.Create();
    private readonly FakeDialogHost dialogs = new();
    private readonly FakeDownloadsClearer downloadsClearer = new();
    private readonly FakeScrapeService scrapeService = new();
    private readonly ScrapeActions actions;

    public GivenScrapeActions()
    {
        var operations = new UserOperationRunner(coordinator, status);
        actions = new(operations, new ClearDownloadsRunner(downloadsClearer, operations, status), new ScrapeRunner(scrapeService, status));
    }

    public void Dispose() => coordinator.Dispose();

    [Fact]
    public async Task when_the_person_confirms_clearing_then_the_downloads_are_cleared_and_the_user_is_told()
    {
        dialogs.Confirmed = true;
        downloadsClearer.Result = Exceptional.Success(new ClearedDownloads(3));

        await actions.ClearDownloadsAsync(dialogs);

        (downloadsClearer.ClearCount, status.Text).ShouldBe((1, $"{TestStatusReporter.Timestamp} Downloads cleared: 3 file records removed and the save directories emptied."));
    }

    [Fact]
    public async Task when_the_person_declines_clearing_then_nothing_is_cleared()
    {
        dialogs.Confirmed = false;

        await actions.ClearDownloadsAsync(dialogs);

        (downloadsClearer.ClearCount, status.Text).ShouldBe((0, string.Empty));
    }

    [Fact]
    public async Task when_the_confirmation_fails_then_the_failure_is_reported_and_not_thrown()
    {
        dialogs.ConfirmFailure = new InvalidOperationException("dialog broke");

        await Should.NotThrowAsync(() => actions.ClearDownloadsAsync(dialogs));

        (downloadsClearer.ClearCount, status.Text).ShouldBe((0, $"{TestStatusReporter.Timestamp} Unable to clear downloads. dialog broke"));
    }

    [Fact]
    public async Task when_the_scraper_is_run_then_its_progress_reaches_the_status()
    {
        scrapeService.OnRun = progress => progress.Report("Fetching categories.");

        await actions.RunScraperAsync(ScrapeSelection.All);

        status.Text.ShouldBe($"{TestStatusReporter.Timestamp} Fetching categories.");
    }

    [Fact]
    public void when_an_operation_is_running_and_it_is_cancelled_then_its_token_is_cancelled()
    {
        _ = coordinator.TryStart(out var cancellationToken);

        actions.CancelOperation();

        cancellationToken.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    public void when_no_operation_is_running_and_cancel_is_requested_then_nothing_happens()
        => Should.NotThrow(actions.CancelOperation);

    private sealed class FakeDownloadsClearer : IDownloadsClearer
    {
        public Exceptional<ClearedDownloads> Result { get; set; } = Exceptional.Success(new ClearedDownloads(0));

        public int ClearCount { get; private set; }

        public Task<Exceptional<ClearedDownloads>> ClearAsync(CancellationToken cancellationToken = default)
        {
            ClearCount++;

            return Task.FromResult(Result);
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
