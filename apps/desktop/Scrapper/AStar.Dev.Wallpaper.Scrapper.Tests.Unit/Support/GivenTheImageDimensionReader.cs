using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;
using SkiaSharp;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenTheImageDimensionReader
{
    private const string Path = "/save/dir/image.png";
    private readonly IImageDimensionReader sut = new ImageDimensionReader();

    private static byte[] CreateValidPngBytes(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    [Fact]
    public void when_the_bytes_are_a_valid_image_then_the_width_is_returned()
    {
        var bytes = CreateValidPngBytes(40, 20);

        sut.Read(bytes, Path).ShouldBeOfType<Ok<ImageDimensions, ScrapeError>>().Value.Width.ShouldBe(40);
    }

    [Fact]
    public void when_the_bytes_are_a_valid_image_then_the_height_is_returned()
    {
        var bytes = CreateValidPngBytes(40, 20);

        sut.Read(bytes, Path).ShouldBeOfType<Ok<ImageDimensions, ScrapeError>>().Value.Height.ShouldBe(20);
    }

    [Fact]
    public void when_the_bytes_are_not_a_valid_image_then_an_image_dimension_read_failed_error_is_returned()
    {
        byte[] invalidBytes = [1, 2, 3, 4,];

        var error = sut.Read(invalidBytes, Path).ShouldBeOfType<Fail<ImageDimensions, ScrapeError>>().Error.ShouldBeOfType<ImageDimensionReadFailed>();

        error.Path.ShouldBe(Path);
    }

    [Fact]
    public void when_the_bytes_are_empty_then_an_image_dimension_read_failed_error_is_returned() =>
        sut.Read([], Path).ShouldBeOfType<Fail<ImageDimensions, ScrapeError>>().Error.ShouldBeOfType<ImageDimensionReadFailed>();
}
