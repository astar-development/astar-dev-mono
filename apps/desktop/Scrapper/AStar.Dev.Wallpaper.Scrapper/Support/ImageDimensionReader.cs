using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using SkiaSharp;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>The production <see cref="IImageDimensionReader" />, backed by <see cref="SKImage" />, frozen from the historical <c>ImagePageService</c> dimension-probing behaviour.</summary>
public sealed class ImageDimensionReader : IImageDimensionReader
{
    /// <inheritdoc />
    public Result<ImageDimensions, ScrapeError> Read(byte[] image, string path)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(path);

        if (image.Length == 0)
            return ScrapeErrorFactory.CreateImageDimensionReadFailed(path, "The image bytes were empty.");

        using var decoded = SKImage.FromEncodedData(image);

        return decoded is null
            ? ScrapeErrorFactory.CreateImageDimensionReadFailed(path, "The image bytes could not be decoded.")
            : ImageDimensionsFactory.Create(decoded.Width, decoded.Height);
    }
}
