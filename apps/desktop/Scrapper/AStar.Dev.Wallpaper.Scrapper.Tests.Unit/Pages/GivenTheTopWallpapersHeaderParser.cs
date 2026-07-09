using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Pages;

public sealed class GivenTheTopWallpapersHeaderParser
{
    [Fact]
    public void when_the_header_text_is_null_then_a_page_parse_failed_error_is_returned() =>
        TopWallpapersHeaderParser.Parse(null)
                                  .ShouldBeOfType<Fail<int, ScrapeError>>()
                                  .Error.ShouldBeOfType<PageParseFailed>();

    [Fact]
    public void when_the_header_text_is_empty_then_a_page_parse_failed_error_is_returned() =>
        TopWallpapersHeaderParser.Parse(string.Empty)
                                  .ShouldBeOfType<Fail<int, ScrapeError>>()
                                  .Error.ShouldBeOfType<PageParseFailed>();

    [Fact]
    public void when_the_header_text_is_garbage_then_a_page_parse_failed_error_is_returned() =>
        TopWallpapersHeaderParser.Parse("garbage text")
                                  .ShouldBeOfType<Fail<int, ScrapeError>>()
                                  .Error.ShouldBeOfType<PageParseFailed>();

    [Fact]
    public void when_the_header_text_has_no_slash_then_a_page_parse_failed_error_is_returned() =>
        TopWallpapersHeaderParser.Parse("Page 5")
                                  .ShouldBeOfType<Fail<int, ScrapeError>>()
                                  .Error.ShouldBeOfType<PageParseFailed>();

    [Theory]
    [InlineData("Page 3 / 42", 42)]
    [InlineData("Page 1 / 1", 1)]
    public void when_the_header_text_is_parseable_then_the_total_page_count_is_returned(string headerText, int expectedPageCount) =>
        TopWallpapersHeaderParser.Parse(headerText).ShouldBeOfType<Ok<int, ScrapeError>>().Value.ShouldBe(expectedPageCount);
}
