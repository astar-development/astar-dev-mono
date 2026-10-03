using System.Diagnostics.CodeAnalysis;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class PagesProcessor(IWallpaperIngestionContextFactory contextFactory, IWallhavenPageFetcher pageFetcher, PageIngestionStep ingestionStep, ScrapeResumePolicy resumePolicy, DownloadPacing pacing, TimeProvider timeProvider) : IPagesProcessor
{
    /// <inheritdoc/>
    public async Task FetchAndProcessPagesAsync(PageScrapeRequest request, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var run = new IngestionRun(request, await contextFactory.CreateAsync(request, cancellationToken), progress, cancellationToken);
        var startPage = await ResolveStartPageAsync(run);
        if (resumePolicy.IsUnchangedSincePreviousScrape(request.PreviousProgress, startPage.Response.Meta))
        {
            progress.Report($"Skipping {request.Label.LogLabel} - nothing has changed since the last scrape.");

            return;
        }

        await IngestPagesAsync(run, startPage);
    }

    private async Task<FetchedPage> ResolveStartPageAsync(IngestionRun run)
    {
        var previousProgress = run.Request.PreviousProgress;
        var resumePage = resumePolicy.ResumePage(previousProgress);
        var fetched = await FetchPageAsync(run, resumePage);
        if (resumePage > 1 && !ScrapeResumePolicy.IsSameCategoryAsPreviousScrape(previousProgress, fetched.Response.Meta)) fetched = await FetchPageAsync(run, 1);

        return fetched;
    }

    /// <summary>Visits each page in turn, waiting before every page after the first so a run of pages that need no downloads does not hit the API rate limit, and counting the pages resumed past as skipped. Once a page is not fully ingested, no later page records progress either, so the next scrape resumes at that page and retries it. Recording itself is left to <see cref="PageIngestionStep"/>; the withheld-progress message is only given when there is stored progress to withhold (not for hot and top, which always restart at page 1).</summary>
    private async Task IngestPagesAsync(IngestionRun run, FetchedPage startPage)
    {
        var current = startPage;
        run.Tally.RecordSkipped((startPage.Number - 1) * startPage.Response.Data.Count);
        var progressWithheld = false;
        while (true)
        {
            var outcome = await ingestionStep.IngestPageAsync(run, current, !progressWithheld);
            if (!progressWithheld && outcome == IngestOutcome.Incomplete && run.Request.PreviousProgress is Option<SearchCategoryProgress>.Some) run.Progress.Report($"Not every wallpaper on page {current.Number} was ingested - progress stays before that page so it is retried on the next scrape.");

            progressWithheld |= outcome == IngestOutcome.Incomplete;
            if (resumePolicy.IsLastPageToVisit(current.Number, current.Response.Meta)) return;

            await Task.Delay(pacing.NextDelay(), timeProvider, run.CancellationToken);
            current = await FetchPageAsync(run, current.Number + 1);
        }
    }

    /// <exception cref="PageFetchException">The page could not be fetched. A cancellation of the scrape is left to propagate.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Every way a page can fail to arrive is wrapped, so a failed page ends only its own search.")]
    private async Task<FetchedPage> FetchPageAsync(IngestionRun run, int page)
    {
        try
        {
            return new(page, await pageFetcher.FetchPageAsync(new PageFetchRequest(run.Request.Label.LogLabel, run.Request.Hooks.PageUrlFactory(page), page), run.Context.Client, run.Progress, run.CancellationToken));
        }
        catch (Exception exception) when (!run.CancellationToken.IsCancellationRequested)
        {
            throw new PageFetchException(exception);
        }
    }
}
