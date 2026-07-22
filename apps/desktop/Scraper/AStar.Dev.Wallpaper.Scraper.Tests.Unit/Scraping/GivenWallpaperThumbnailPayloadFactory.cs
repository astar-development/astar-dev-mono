using AStar.Dev.Wallpaper.Scraper.Scraping;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperThumbnailPayloadFactory
{
    private static readonly byte[] thumbnailBytes = [1, 2, 3];

    [Fact]
    public void when_the_category_name_and_tags_are_present_then_they_are_carried_through_unchanged()
    {
        var payload = WallpaperThumbnailPayloadFactory.Create(thumbnailBytes, "Nature", ["forest", "mountains"]);

        payload.Bytes.ShouldBe(thumbnailBytes);
        payload.CategoryName.ShouldBe("Nature");
        payload.Tags.ShouldBe(["forest", "mountains"]);
    }

    [Fact]
    public void when_the_category_name_is_null_then_it_is_normalized_to_uncategorized()
    {
        var payload = WallpaperThumbnailPayloadFactory.Create(thumbnailBytes, null, []);

        payload.CategoryName.ShouldBe("Uncategorized");
    }

    [Fact]
    public void when_the_category_name_is_whitespace_then_it_is_normalized_to_uncategorized()
    {
        var payload = WallpaperThumbnailPayloadFactory.Create(thumbnailBytes, "   ", []);

        payload.CategoryName.ShouldBe("Uncategorized");
    }

    [Fact]
    public void when_the_category_name_has_surrounding_whitespace_then_it_is_trimmed()
    {
        var payload = WallpaperThumbnailPayloadFactory.Create(thumbnailBytes, "  Nature  ", []);

        payload.CategoryName.ShouldBe("Nature");
    }

    [Fact]
    public void when_the_tags_are_null_then_they_are_normalized_to_an_empty_list()
    {
        var payload = WallpaperThumbnailPayloadFactory.Create(thumbnailBytes, "Nature", null);

        payload.Tags.ShouldBeEmpty();
    }
}
