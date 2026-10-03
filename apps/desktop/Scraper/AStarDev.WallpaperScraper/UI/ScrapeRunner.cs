using System.Diagnostics.CodeAnalysis;
using AStarDev.WallpaperScraper.Scraping;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>Starts a scrape from the main window, showing its progress in the status and reporting any failure instead of letting it escape the button handler.</summary>
/// <param name="scrapeService">The service that performs the scrape.</param>
/// <param name="status">Where progress and failures are reported.</param>
public sealed class ScrapeRunner(IScrapeService scrapeService, StatusReporter status)
{
    /// <summary>Runs the scraper on the thread pool so its continuations never resume on the UI thread; progress reaches the UI through the status dispatch.</summary>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Called from an async void UI handler: any exception escaping it would crash the application, so every failure is reported in the status instead.")]
    /// <param name="selection">Which of the scrapes to run.</param>
    public async Task RunAsync(ScrapeSelection selection)
    {
        try
        {
            await Task.Run(() => scrapeService.RunScraperAsync(selection, new StatusProgress(status)));
        }
        catch (Exception exception)
        {
            status.Error("The scrape failed.", exception);
        }
    }

    private sealed class StatusProgress(StatusReporter status) : IProgress<string>
    {
        public void Report(string value) => status.Append(value);
    }
}
