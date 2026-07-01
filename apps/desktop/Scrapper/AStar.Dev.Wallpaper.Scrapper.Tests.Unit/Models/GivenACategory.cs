using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Models;

public sealed class GivenACategory
{
    private const int KnownImageCount = 48;
    private const int KnownPageCount  = 2;

    [Fact]
    public void when_image_count_and_page_count_both_match_then_is_up_to_date_returns_true()
    {
        var sut = new Category { LastKnownImageCount = KnownImageCount, TotalPages = KnownPageCount };

        var result = sut.IsUpToDate(KnownImageCount, KnownPageCount);

        result.ShouldBeTrue();
    }

    [Fact]
    public void when_image_count_differs_then_is_up_to_date_returns_false()
    {
        var sut = new Category { LastKnownImageCount = KnownImageCount, TotalPages = KnownPageCount };

        var result = sut.IsUpToDate(KnownImageCount + 1, KnownPageCount);

        result.ShouldBeFalse();
    }

    [Fact]
    public void when_page_count_differs_then_is_up_to_date_returns_false()
    {
        var sut = new Category { LastKnownImageCount = KnownImageCount, TotalPages = KnownPageCount };

        var result = sut.IsUpToDate(KnownImageCount, KnownPageCount + 1);

        result.ShouldBeFalse();
    }

    [Fact]
    public void when_both_image_count_and_page_count_differ_then_is_up_to_date_returns_false()
    {
        var sut = new Category { LastKnownImageCount = KnownImageCount, TotalPages = KnownPageCount };

        var result = sut.IsUpToDate(KnownImageCount + 24, KnownPageCount + 1);

        result.ShouldBeFalse();
    }

    [Fact]
    public void when_category_is_unvisited_and_site_returns_zero_then_is_up_to_date_returns_true()
    {
        var sut = new Category { LastKnownImageCount = 0, TotalPages = 0 };

        var result = sut.IsUpToDate(0, 0);

        result.ShouldBeTrue();
    }

    [Fact]
    public void when_category_is_unvisited_and_site_has_content_then_is_up_to_date_returns_false()
    {
        var sut = new Category { LastKnownImageCount = 0, TotalPages = 0 };

        var result = sut.IsUpToDate(24, 1);

        result.ShouldBeFalse();
    }
}
