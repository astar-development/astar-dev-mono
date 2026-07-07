using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Pages;

public sealed class GivenTheSubscriptionHeaderParser
{
    [Fact]
    public void when_the_header_text_is_null_then_a_page_parse_failed_error_is_returned() =>
        SubscriptionHeaderParser.Parse(null)
                                 .ShouldBeOfType<Fail<PageInfo, ScrapeError>>()
                                 .Error.ShouldBeOfType<PageParseFailed>();

    [Fact]
    public void when_the_header_text_is_empty_then_a_page_parse_failed_error_is_returned() =>
        SubscriptionHeaderParser.Parse(string.Empty)
                                 .ShouldBeOfType<Fail<PageInfo, ScrapeError>>()
                                 .Error.ShouldBeOfType<PageParseFailed>();

    [Fact]
    public void when_the_header_text_does_not_start_with_a_number_then_a_page_parse_failed_error_is_returned() =>
        SubscriptionHeaderParser.Parse("abc New Subscription Wallpapers")
                                 .ShouldBeOfType<Fail<PageInfo, ScrapeError>>()
                                 .Error.ShouldBeOfType<PageParseFailed>();

    [Theory]
    [InlineData("48 New Subscription Wallpapers", 2, 48, "New-Subscription-Wallpapers")]
    [InlineData("1 New Subscription Wallpapers", 1, 1, "New-Subscription-Wallpapers")]
    [InlineData("1,000 New Subscription Wallpapers", 42, 1000, "New-Subscription-Wallpapers")]
    public void when_the_header_text_is_parseable_then_the_page_info_is_correctly_derived(string headerText, int expectedPageCount, int expectedImageCount, string expectedSubDirectoryName)
    {
        var pageInfo = SubscriptionHeaderParser.Parse(headerText).ShouldBeOfType<Ok<PageInfo, ScrapeError>>().Value;

        pageInfo.PageCount.ShouldBe(expectedPageCount);
        pageInfo.ImageCount.ShouldBe(expectedImageCount);
        pageInfo.SubDirectoryName.ShouldBe(expectedSubDirectoryName);
    }

    [Fact]
    public void when_the_header_has_no_new_keyword_then_the_sub_directory_name_is_empty() =>
        SubscriptionHeaderParser.Parse("48 Something Else")
                                 .ShouldBeOfType<Ok<PageInfo, ScrapeError>>()
                                 .Value.SubDirectoryName.ShouldBe(string.Empty);
}
