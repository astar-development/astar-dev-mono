using AStar.Dev.Wallpaper.Scraper.Scraping;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenTagDataFactory
{
    [Fact]
    public void when_the_tag_has_surrounding_whitespace_then_it_is_trimmed()
    {
        var tagData = TagDataFactory.Create("  Nature  ", "outdoors");

        tagData.Tag.ShouldBe("Nature");
    }

    [Fact]
    public void when_the_category_is_null_then_it_remains_null()
    {
        var tagData = TagDataFactory.Create("Nature", null);

        tagData.Category.ShouldBeNull();
    }

    [Fact]
    public void when_the_category_is_whitespace_then_it_is_normalized_to_null()
    {
        var tagData = TagDataFactory.Create("Nature", "   ");

        tagData.Category.ShouldBeNull();
    }

    [Fact]
    public void when_the_category_has_surrounding_whitespace_then_it_is_trimmed()
    {
        var tagData = TagDataFactory.Create("Nature", "  outdoors  ");

        tagData.Category.ShouldBe("outdoors");
    }
}
