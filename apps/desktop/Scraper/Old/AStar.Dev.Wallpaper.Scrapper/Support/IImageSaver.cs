using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>Saves a scraped image's bytes to disk.</summary>
public interface IImageSaver
{
    /// <summary>Saves <paramref name="image" /> to <paramref name="path" />, cleaning the path first.</summary>
    Task<Result<Unit, ScrapeError>> SaveAsync(byte[] image, string path);
}
