using System.Diagnostics.CodeAnalysis;
using AStarDev.ControlDb;
using AStarDev.WallpaperScraper.WallpaperIngestion;
using AStarDev.Utilities;

namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>Ingests one fetched page: its wallpapers, the progress report and the save that commits them.</summary>
/// <param name="wallpaperIngestionService">Ingests the wallpapers on a page.</param>
/// <param name="unitOfWork">Saves what the ingestion recorded.</param>
public sealed class PageIngestionStep(IWallpaperIngestionService wallpaperIngestionService, IUnitOfWork unitOfWork)
{
    /// <summary>Ingests the page, reports the progress so far through the request's hook (only when the page was fully ingested and <paramref name="recordProgress"/> allows it), then saves. When the scrape is cancelled part-way, the wallpapers downloaded so far are saved before the cancellation propagates.</summary>
    /// <param name="run">The scrape the page belongs to.</param>
    /// <param name="page">The page to ingest.</param>
    /// <param name="recordProgress">False once an earlier page of this scrape was not fully ingested, so progress never moves past a page that must be retried.</param>
    /// <returns>Whether the page was fully ingested.</returns>
    public async Task<IngestOutcome> IngestPageAsync(IngestionRun run, FetchedPage page, bool recordProgress)
    {
        run.Tally.Total = page.Response.Meta.Total;
        run.Tally.CurrentPage = page.Number;
        run.Tally.TotalPages = page.Response.Meta.LastPage;
        try
        {
            var outcome = await wallpaperIngestionService.IngestPageAsync(page.Response.Data, run);
            if (recordProgress && outcome == IngestOutcome.Complete) run.Request.Hooks.OnPageCompleted(new SearchCategoryProgress(page.Response.Meta.Total, page.Number, page.Response.Meta.LastPage));
            _ = await unitOfWork.SaveChangesAsync(run.CancellationToken);

            return outcome;
        }
        catch (OperationCanceledException) when (run.CancellationToken.IsCancellationRequested)
        {
            await SavePartiallyIngestedPageAsync(run.Progress);

            throw;
        }
    }

    /// <summary>Saves the wallpapers downloaded so far on a page that was cancelled part-way, ignoring the cancellation that interrupted it.</summary>
    /// <param name="progress">Receives the outcome.</param>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Any failure of this best-effort save must be reported rather than replace the cancellation that is already propagating.")]
    private async Task SavePartiallyIngestedPageAsync(IProgress<string> progress)
    {
        try
        {
            _ = await unitOfWork.SaveChangesAsync(CancellationToken.None);
            progress.Report("Scrape cancelled - saved wallpapers downloaded so far this page.");
        }
        catch (Exception ex)
        {
            progress.Report($"Scrape cancelled - failed to save wallpapers downloaded so far this page: {ex.ToMessageChain()}");
        }
    }
}
