using AStar.Dev.Wallpaper.Scrapper.Pages;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Pages;

public sealed class GivenTheImageLinkSelector
{
    [Fact]
    public void when_the_hrefs_are_empty_then_no_links_are_selected() =>
        ImageLinkSelector.SelectWanted([]).ShouldBeEmpty();

    [Fact]
    public void when_an_href_is_null_then_it_is_not_selected() =>
        ImageLinkSelector.SelectWanted([null, "https://example.test/w/1"]).ShouldBe(["https://example.test/w/1"]);

    [Fact]
    public void when_an_href_does_not_contain_the_wanted_segment_then_it_is_not_selected() =>
        ImageLinkSelector.SelectWanted(["https://example.test/other/1", "https://example.test/w/1"]).ShouldBe(["https://example.test/w/1"]);

    [Fact]
    public void when_there_are_more_than_the_images_per_page_limit_then_the_result_is_truncated_to_the_limit()
    {
        var hrefs = Enumerable.Range(1, 30).Select(i => $"https://example.test/w/{i}");

        ImageLinkSelector.SelectWanted(hrefs).Count.ShouldBe(24);
    }

    [Fact]
    public void when_there_are_more_than_the_images_per_page_limit_then_the_first_matching_links_are_kept_in_order()
    {
        var hrefs = Enumerable.Range(1, 30).Select(i => $"https://example.test/w/{i}");

        ImageLinkSelector.SelectWanted(hrefs).ShouldBe(Enumerable.Range(1, 24).Select(i => $"https://example.test/w/{i}"));
    }

    [Fact]
    public void when_the_wanted_hrefs_are_within_the_limit_then_their_order_is_preserved() =>
        ImageLinkSelector.SelectWanted(["https://example.test/w/3", "https://example.test/w/1", "https://example.test/w/2"])
                          .ShouldBe(["https://example.test/w/3", "https://example.test/w/1", "https://example.test/w/2"]);
}
