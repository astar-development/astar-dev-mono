using AStar.Dev.File.App.ViewModels;

namespace AStar.Dev.File.App.Services;

/// <summary>
/// Service for handling file viewing operations including updating view history.
/// </summary>
public interface IFileViewerService
{
    /// <summary>
    /// Raised when a file is requested to be viewed.
    /// </summary>
    event Action<ScannedFileDisplayItem>? FileViewRequested;

    /// <summary>
    /// Processes a file view request, updating the last viewed timestamp and raising the view event.
    /// </summary>
    /// <param name="item">The file to view</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ViewFileAsync(ScannedFileDisplayItem? item);
}
