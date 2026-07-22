namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Reads the pixel dimensions of an encoded image.
/// </summary>
public interface IImageDimensionsReader
{
    /// <summary>
    ///     Decodes the supplied image bytes and reads its width and height.
    /// </summary>
    /// <param name="imageBytes">The encoded image bytes.</param>
    ImageDimensions Read(byte[] imageBytes);
}
