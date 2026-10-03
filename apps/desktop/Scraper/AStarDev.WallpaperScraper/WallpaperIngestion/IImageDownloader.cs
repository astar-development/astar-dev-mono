namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>Downloads a wallpaper's image and saves it to disk.</summary>
public interface IImageDownloader
{
    /// <summary>Downloads the wallpaper's image and writes it under the request's file name, creating the request's directory if it does not exist.</summary>
    /// <param name="request">The wallpaper, and the file name and directory to save its image under.</param>
    /// <param name="progress">The progress reporter to report the download progress.</param>
    /// <param name="client">The HTTP client used to make the request.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The full path the image was saved to.</returns>
    /// <exception cref="HttpRequestException">The server did not respond with a success status; nothing is written.</exception>
    Task<string> DownloadAsync(WallpaperFileRequest request, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken);
}
