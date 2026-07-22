using SkiaSharp;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Generates a bounded, rounded-corner thumbnail from a downloaded wallpaper's full-size image bytes using SkiaSharp.
/// </summary>
public sealed class WallpaperThumbnailGenerator : IWallpaperThumbnailGenerator
{
    private const int MaxDimension = 750;
    private const float CornerRadius = 10f;

    /// <inheritdoc />
    public byte[] Generate(byte[] imageBytes)
    {
        using var source = SKBitmap.Decode(imageBytes);
        using var resized = Resize(source);
        using var rounded = ApplyRoundedCorners(resized);
        using var thumbnail = SKImage.FromBitmap(rounded);
        using var encoded = thumbnail.Encode(SKEncodedImageFormat.Png, 100);

        return encoded.ToArray();
    }

    private static SKBitmap Resize(SKBitmap source)
    {
        float scale = Math.Min(1f, MaxDimension / (float)Math.Max(source.Width, source.Height));

        if (scale >= 1f)
        {
            return source.Copy();
        }

        int width = Math.Max(1, (int)Math.Round(source.Width * scale));
        int height = Math.Max(1, (int)Math.Round(source.Height * scale));

        return source.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
    }

    private static SKBitmap ApplyRoundedCorners(SKBitmap source)
    {
        var rounded = new SKBitmap(source.Width, source.Height);
        using var canvas = new SKCanvas(rounded);
        using var paint = new SKPaint { IsAntialias = true, };
        using var path = new SKPath();

        path.AddRoundRect(new SKRoundRect(new SKRect(0, 0, source.Width, source.Height), CornerRadius, CornerRadius));
        canvas.ClipPath(path, antialias: true);
        canvas.DrawBitmap(source, 0, 0, paint);

        return rounded;
    }
}
