using System.IO.Abstractions;
using SkiaSharp;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public sealed class DownloadedImageDecoder(IFileSystem fileSystem) : IDownloadedImageDecoder
{
    /// <inheritdoc/>
    public Stream DecodeToPng(string filePath, int maxDimension)
    {
        using var fileStream = fileSystem.File.OpenRead(filePath);
        using var codec = SKCodec.Create(fileStream, out var codecResult);
        if (codecResult != SKCodecResult.Success) throw UnableToDecode(filePath);

        var target = FitWithin(codec.Info.Width, codec.Info.Height, maxDimension);

        // Ask the codec for the smallest size it can produce cheaply (for example JPEG's DCT scaling) so a
        // full-resolution bitmap is never materialised just to be shrunk.
        var decodeSize = codec.GetScaledDimensions((float)target.Width / codec.Info.Width);
        using var decoded = SKBitmap.Decode(codec, new SKImageInfo(decodeSize.Width, decodeSize.Height)) ?? throw UnableToDecode(filePath);
        using var resized = decoded.Width == target.Width && decoded.Height == target.Height ? null : decoded.Resize(new SKImageInfo(target.Width, target.Height), SKSamplingOptions.Default);
        using var image = SKImage.FromBitmap(resized ?? decoded);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        var pngStream = new MemoryStream();
        data.SaveTo(pngStream);
        pngStream.Position = 0;

        return pngStream;
    }

    private static SKSizeI FitWithin(int width, int height, int maxDimension)
    {
        var scale = Math.Min(1d, (double)maxDimension / Math.Max(width, height));

        return new SKSizeI(Math.Max(1, (int)Math.Round(width * scale)), Math.Max(1, (int)Math.Round(height * scale)));
    }

    private static InvalidOperationException UnableToDecode(string filePath) => new($"Unable to decode '{filePath}' as an image.");
}
