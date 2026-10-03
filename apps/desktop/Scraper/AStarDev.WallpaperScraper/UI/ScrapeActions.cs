using AStarDev.WallpaperScraper.Scraping;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>The scraping actions the main window's buttons trigger: run, cancel and clear the downloads.</summary>
public sealed class ScrapeActions(UserOperationRunner operations, ClearDownloadsRunner clearDownloadsRunner, ScrapeRunner scrapeRunner)
{
    /// <summary>Clears the downloads after the person confirms.</summary>
    /// <param name="dialogs">The dialogs the person is asked through.</param>
    public async Task ClearDownloadsAsync(IDialogHost dialogs)
        => await operations.ReportFailuresAsync("Unable to clear downloads.", async () =>
        {
            var confirmed = await dialogs.ConfirmAsync(
                "Clear Downloads",
                "This permanently deletes every file record and everything inside the base save and famous directories. Nothing else is affected. This cannot be undone. Continue?",
                "Clear Downloads");
            if (confirmed) await clearDownloadsRunner.RunAsync();
        });

    /// <summary>Runs the scraper.</summary>
    /// <param name="selection">Which of the scrapes to run.</param>
    public async Task RunScraperAsync(ScrapeSelection selection) => await scrapeRunner.RunAsync(selection);

    /// <summary>Cancels the operation in progress.</summary>
    public void CancelOperation() => operations.Cancel();
}
