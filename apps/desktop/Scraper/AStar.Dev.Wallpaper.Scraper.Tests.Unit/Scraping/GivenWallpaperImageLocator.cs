using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperImageLocator
{
    [Fact]
    public async Task when_the_wallpaper_element_has_a_source_then_it_is_returned_as_some()
    {
        var page = CreatePageWithImageSource("https://wallhaven.cc/images/pic.jpg");
        var sut = new WallpaperImageLocator();

        var result = await sut.LocateAsync(page, TestContext.Current.CancellationToken);

        result.ShouldBe(new Option<string>.Some("https://wallhaven.cc/images/pic.jpg"));
    }

    [Fact]
    public async Task when_the_wallpaper_element_has_no_source_then_none_is_returned()
    {
        var page = CreatePageWithImageSource(null);
        var sut = new WallpaperImageLocator();

        var result = await sut.LocateAsync(page, TestContext.Current.CancellationToken);

        result.ShouldBe(Option<string>.None.Instance);
    }

    [Fact]
    public async Task when_the_wallpaper_element_source_is_only_whitespace_then_none_is_returned()
    {
        var page = CreatePageWithImageSource("   ");
        var sut = new WallpaperImageLocator();

        var result = await sut.LocateAsync(page, TestContext.Current.CancellationToken);

        result.ShouldBe(Option<string>.None.Instance);
    }

    private static IPage CreatePageWithImageSource(string? src)
    {
        var page = Substitute.For<IPage>();
        var imageTag = Substitute.For<ILocator>();
        imageTag.GetAttributeAsync("src").Returns(Task.FromResult(src));
        page.Locator("#wallpaper").Returns(imageTag);

        return page;
    }
}
