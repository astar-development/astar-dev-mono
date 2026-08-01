using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>Retrieves the raw bytes of a scraped image.</summary>
public interface IImageRetriever
{
    /// <summary>Downloads the image at <paramref name="url" />.</summary>
    Task<Result<byte[], ScrapeError>> GetImageAsync(string url, CancellationToken cancellationToken = default);
}
