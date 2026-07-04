using AStar.Dev.Wallpaper.Scrapper.Support;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenAScrapedFileNameFactory
{
    [Fact]
    public void when_a_prefix_is_supplied_then_the_name_is_the_lowercased_prefix_and_filename()
    {
        string result = ScrapedFileNameFactory.Create("Azami", "https://w.wallhaven.cc/full/p9/Wallhaven-P9ldle.JPG");

        result.ShouldBe("azami wallhaven-p9ldle.jpg");
    }

    [Fact]
    public void when_no_prefix_is_supplied_then_the_name_is_the_lowercased_filename()
    {
        string result = ScrapedFileNameFactory.Create(string.Empty, "https://w.wallhaven.cc/full/p9/Wallhaven-P9ldle.JPG");

        result.ShouldBe("wallhaven-p9ldle.jpg");
    }

    [Fact]
    public void when_the_prefix_is_null_then_the_name_is_the_lowercased_filename()
    {
        string result = ScrapedFileNameFactory.Create(null, "https://w.wallhaven.cc/full/p9/wallhaven-p9ldle.jpg");

        result.ShouldBe("wallhaven-p9ldle.jpg");
    }
}
