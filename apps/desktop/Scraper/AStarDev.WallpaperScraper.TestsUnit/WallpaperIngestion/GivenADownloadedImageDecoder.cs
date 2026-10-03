using AStarDev.WallpaperScraper.WallpaperIngestion;
using SkiaSharp;
using Testably.Abstractions.Testing;

namespace AStarDev.WallpaperScraper.TestsUnit.WallpaperIngestion;

public sealed class GivenADownloadedImageDecoder
{
    private static readonly byte[] OnePixelPng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGP4z8AAAAMBAQDJ/pLvAAAAAElFTkSuQmCC");
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly MockFileSystem fileSystem = new();
    private readonly DownloadedImageDecoder decoder;

    public GivenADownloadedImageDecoder()
        => decoder = new(fileSystem);

    [Fact]
    public void when_decoding_a_valid_image_file_then_a_png_encoded_stream_is_returned()
    {
        fileSystem.Directory.CreateDirectory("/images");
        fileSystem.File.WriteAllBytes("/images/wallpaper-1.png", OnePixelPng);

        using var result = decoder.DecodeToPng("/images/wallpaper-1.png", 1280);

        var signature = new byte[8];
        result.ReadExactly(signature);
        signature.ShouldBe(PngSignature);
    }

    [Fact]
    public void when_decoding_a_file_that_is_not_an_image_then_it_throws()
    {
        fileSystem.Directory.CreateDirectory("/images");
        fileSystem.File.WriteAllBytes("/images/not-an-image.txt", "this is not an image"u8.ToArray());

        Should.Throw<InvalidOperationException>(() => decoder.DecodeToPng("/images/not-an-image.txt", 1280));
    }

    [Theory]
    [InlineData(4000, 2000, 100, 50)]
    [InlineData(2000, 4000, 50, 100)]
    [InlineData(300, 300, 100, 100)]
    public void when_the_image_is_larger_than_the_maximum_dimension_then_it_is_downscaled_preserving_its_aspect_ratio(int width, int height, int expectedWidth, int expectedHeight)
    {
        WriteImage("/images/large.png", width, height);

        using var result = decoder.DecodeToPng("/images/large.png", 100);

        DimensionsOf(result).ShouldBe((expectedWidth, expectedHeight));
    }

    [Fact]
    public void when_the_image_is_smaller_than_the_maximum_dimension_then_it_is_not_upscaled()
    {
        WriteImage("/images/small.png", 60, 40);

        using var result = decoder.DecodeToPng("/images/small.png", 100);

        DimensionsOf(result).ShouldBe((60, 40));
    }

    private void WriteImage(string path, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        fileSystem.Directory.CreateDirectory("/images");
        fileSystem.File.WriteAllBytes(path, data.ToArray());
    }

    private static (int Width, int Height) DimensionsOf(Stream png)
    {
        using var bitmap = SKBitmap.Decode(png);

        return (bitmap.Width, bitmap.Height);
    }
}
