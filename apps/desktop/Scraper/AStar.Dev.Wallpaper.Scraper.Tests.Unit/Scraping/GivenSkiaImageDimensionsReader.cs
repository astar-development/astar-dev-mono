using AStar.Dev.Wallpaper.Scraper.Scraping;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenSkiaImageDimensionsReader
{
    private const string OnePixelPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    [Fact]
    public void when_a_valid_image_is_decoded_then_its_pixel_dimensions_are_returned()
    {
        byte[] imageBytes = Convert.FromBase64String(OnePixelPngBase64);
        var sut = new SkiaImageDimensionsReader();

        var dimensions = sut.Read(imageBytes);

        dimensions.ShouldBe(new ImageDimensions(1, 1));
    }
}
