using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperCountReader
{
    [Fact]
    public async Task when_the_header_reports_a_wallpaper_count_then_it_is_parsed_and_returned()
    {
        var page = CreatePageWithHeaderText("50 Wallpapers found for \"nature\"");
        var sut = new WallpaperCountReader();

        int count = await sut.ReadAsync(page, TestContext.Current.CancellationToken);

        count.ShouldBe(50);
    }

    [Fact]
    public async Task when_the_header_reports_a_comma_separated_wallpaper_count_then_the_commas_are_ignored()
    {
        var page = CreatePageWithHeaderText("1,234 Wallpapers found for \"nature\"");
        var sut = new WallpaperCountReader();

        int count = await sut.ReadAsync(page, TestContext.Current.CancellationToken);

        count.ShouldBe(1234);
    }

    [Fact]
    public async Task when_the_header_text_does_not_start_with_a_number_then_zero_is_returned()
    {
        var page = CreatePageWithHeaderText("No Wallpapers found for \"nature\"");
        var sut = new WallpaperCountReader();

        int count = await sut.ReadAsync(page, TestContext.Current.CancellationToken);

        count.ShouldBe(0);
    }

    private static IPage CreatePageWithHeaderText(string headerText)
    {
        var page = Substitute.For<IPage>();
        var header = Substitute.For<ILocator>();
        header.AllTextContentsAsync().Returns(Task.FromResult<IReadOnlyList<string>>([headerText]));
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(header);

        return page;
    }
}
