namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>Interface for decoding a downloaded wallpaper image file into a PNG-encoded stream suitable for display.</summary>
public interface IDownloadedImageDecoder
{
    /// <summary>Decodes the image at <paramref name="filePath"/>, downscales it to fit within <paramref name="maxDimension"/> (never upscaling) and re-encodes it as PNG.</summary>
    /// <param name="filePath">The path of the image file to decode.</param>
    /// <param name="maxDimension">The maximum width or height, in pixels, of the returned image.</param>
    /// <returns>A stream, positioned at the start, containing the PNG-encoded image.</returns>
    /// <exception cref="InvalidOperationException">The file's content could not be decoded as an image.</exception>
    Stream DecodeToPng(string filePath, int maxDimension);
}
