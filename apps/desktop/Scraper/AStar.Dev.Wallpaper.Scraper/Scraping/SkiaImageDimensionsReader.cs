using SkiaSharp;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Reads the pixel dimensions of an encoded image using SkiaSharp.
/// </summary>
public sealed class SkiaImageDimensionsReader : IImageDimensionsReader
{
    /// <inheritdoc />
    public ImageDimensions Read(byte[] imageBytes)
    {
        using var image = SKImage.FromEncodedData(imageBytes);

        return ImageDimensionsFactory.Create(image.Width, image.Height);
    }
}
