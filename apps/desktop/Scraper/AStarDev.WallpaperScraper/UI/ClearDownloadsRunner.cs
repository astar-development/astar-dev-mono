using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Downloads;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>Clears the downloads for the main window as the single active operation, telling the user what was removed and reporting any failure instead of letting it escape the menu handler. The confirmation is the caller's job.</summary>
/// <param name="downloadsClearer">The service that clears the file records and the save directories.</param>
/// <param name="operations">Runs the clear as the single active operation.</param>
/// <param name="status">Where the outcome is reported.</param>
public sealed class ClearDownloadsRunner(IDownloadsClearer downloadsClearer, UserOperationRunner operations, StatusReporter status)
{
    /// <summary>Clears the downloads unless another operation is already running.</summary>
    public Task RunAsync() =>
        operations.RunAsync("Clear Downloads cancelled.", "Unable to clear downloads.", async cancellationToken =>
        {
            var cleared = (await downloadsClearer.ClearAsync(cancellationToken)).GetOrThrow();
            status.Append($"Downloads cleared: {cleared.FileRecords} file records removed and the save directories emptied.");
        });
}
