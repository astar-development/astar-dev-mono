using AStar.Dev.Wallpaper.Scraper.Scraping;
using SkiaSharp;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperThumbnailGenerator
{
    private readonly WallpaperThumbnailGenerator sut = new();

    [Fact]
    public void when_the_source_image_exceeds_the_max_dimension_then_it_is_scaled_down_proportionally()
    {
        byte[] imageBytes = CreateOpaqueSquarePng(1_000, 750);

        byte[] thumbnailBytes = sut.Generate(imageBytes);

        using var thumbnail = SKBitmap.Decode(thumbnailBytes);
        thumbnail.Width.ShouldBe(750);
        thumbnail.Height.ShouldBe(562);
    }

    [Fact]
    public void when_the_source_image_is_within_the_max_dimension_then_it_is_not_upscaled()
    {
        byte[] imageBytes = CreateOpaqueSquarePng(100, 50);

        byte[] thumbnailBytes = sut.Generate(imageBytes);

        using var thumbnail = SKBitmap.Decode(thumbnailBytes);
        thumbnail.Width.ShouldBe(100);
        thumbnail.Height.ShouldBe(50);
    }

    [Fact]
    public void when_a_thumbnail_is_generated_then_its_corners_are_rounded_transparent()
    {
        byte[] imageBytes = CreateOpaqueSquarePng(100, 100);

        byte[] thumbnailBytes = sut.Generate(imageBytes);

        using var thumbnail = SKBitmap.Decode(thumbnailBytes);
        thumbnail.GetPixel(0, 0).Alpha.ShouldBe((byte)0);
        thumbnail.GetPixel(50, 50).Alpha.ShouldBe((byte)255);
    }

    private static byte[] CreateOpaqueSquarePng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Red);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }
}
