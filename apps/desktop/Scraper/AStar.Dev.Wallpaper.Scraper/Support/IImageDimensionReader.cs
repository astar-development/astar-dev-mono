using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;

namespace AStar.Dev.Wallpaper.Scraper.Support;

/// <summary>Reads the pixel dimensions of a downloaded image's bytes.</summary>
public interface IImageDimensionReader
{
    /// <summary>Reads the dimensions of <paramref name="image" />, previously saved to <paramref name="path" />.</summary>
    Result<ImageDimensions, ScrapeError> Read(byte[] image, string path);
}
