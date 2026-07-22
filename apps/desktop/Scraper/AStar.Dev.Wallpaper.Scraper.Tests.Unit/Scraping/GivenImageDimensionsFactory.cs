using AStar.Dev.Wallpaper.Scraper.Scraping;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenImageDimensionsFactory
{
    [Fact]
    public void when_width_and_height_are_positive_then_they_are_passed_through_unchanged()
    {
        var dimensions = ImageDimensionsFactory.Create(1920, 1080);

        dimensions.ShouldBe(new ImageDimensions(1920, 1080));
    }

    [Fact]
    public void when_width_is_negative_then_it_is_normalized_to_zero()
    {
        var dimensions = ImageDimensionsFactory.Create(-1, 1080);

        dimensions.ShouldBe(new ImageDimensions(0, 1080));
    }

    [Fact]
    public void when_height_is_negative_then_it_is_normalized_to_zero()
    {
        var dimensions = ImageDimensionsFactory.Create(1920, -1);

        dimensions.ShouldBe(new ImageDimensions(1920, 0));
    }

    [Fact]
    public void when_width_and_height_are_both_negative_then_both_are_normalized_to_zero()
    {
        var dimensions = ImageDimensionsFactory.Create(-100, -200);

        dimensions.ShouldBe(new ImageDimensions(0, 0));
    }

    [Fact]
    public void when_width_and_height_are_zero_then_they_are_passed_through_unchanged()
    {
        var dimensions = ImageDimensionsFactory.Create(0, 0);

        dimensions.ShouldBe(new ImageDimensions(0, 0));
    }
}
