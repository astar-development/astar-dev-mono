using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Pages;

public sealed class GivenThePageHeaderParser
{
    [Fact]
    public void when_the_header_text_is_null_then_a_page_parse_failed_error_is_returned() =>
        PageHeaderParser.Parse(null)
                         .ShouldBeOfType<Fail<PageInfo, ScrapeError>>()
                         .Error.ShouldBeOfType<PageParseFailed>();

    [Fact]
    public void when_the_header_text_is_empty_then_a_page_parse_failed_error_is_returned() =>
        PageHeaderParser.Parse(string.Empty)
                         .ShouldBeOfType<Fail<PageInfo, ScrapeError>>()
                         .Error.ShouldBeOfType<PageParseFailed>();

    [Fact]
    public void when_the_header_text_does_not_start_with_a_number_then_a_page_parse_failed_error_is_returned() =>
        PageHeaderParser.Parse("abc Wallpapers found for #bad")
                         .ShouldBeOfType<Fail<PageInfo, ScrapeError>>()
                         .Error.ShouldBeOfType<PageParseFailed>();

    [Fact]
    public void when_the_image_count_has_two_commas_then_the_comma_stripped_slice_quirk_causes_a_page_parse_failed_error() =>
        PageHeaderParser.Parse("1,234,567 Wallpapers found for #huge")
                         .ShouldBeOfType<Fail<PageInfo, ScrapeError>>()
                         .Error.ShouldBeOfType<PageParseFailed>();

    [Theory]
    [InlineData("1,234 Wallpapers found for #nature", 52, 1234, "nature")]
    [InlineData("1 Wallpapers found for #x", 1, 1, "x")]
    [InlineData("24 Wallpapers found for #Exact", 1, 24, "Exact")]
    [InlineData("25 Wallpapers found for #Rounding", 2, 25, "Rounding")]
    [InlineData("12,345 Wallpapers found for #medium", 515, 12345, "medium")]
    public void when_the_header_text_is_parseable_then_the_page_info_is_correctly_derived(string headerText, int expectedPageCount, int expectedImageCount, string expectedSubDirectoryName)
    {
        var pageInfo = PageHeaderParser.Parse(headerText).ShouldBeOfType<Ok<PageInfo, ScrapeError>>().Value;

        pageInfo.PageCount.ShouldBe(expectedPageCount);
        pageInfo.ImageCount.ShouldBe(expectedImageCount);
        pageInfo.SubDirectoryName.ShouldBe(expectedSubDirectoryName);
    }

    [Fact]
    public void when_the_header_has_no_for_keyword_then_the_sub_directory_name_reflects_the_absent_for_quirk() =>
        PageHeaderParser.Parse("500 Wallpapers found")
                         .ShouldBeOfType<Ok<PageInfo, ScrapeError>>()
                         .Value.SubDirectoryName.ShouldBe("-Wallpapers-found");

    [Theory]
    [InlineData("10,000 Wallpapers found for #Big Category", "Big-Category")]
    public void when_the_sub_directory_name_contains_spaces_and_a_hash_then_they_are_replaced(string headerText, string expectedSubDirectoryName) =>
        PageHeaderParser.Parse(headerText)
                         .ShouldBeOfType<Ok<PageInfo, ScrapeError>>()
                         .Value.SubDirectoryName.ShouldBe(expectedSubDirectoryName);
}
