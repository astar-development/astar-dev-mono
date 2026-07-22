using AStar.Dev.Wallpaper.Scraper.Scraping;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenSearchCategoryProgressFactory
{
    [Fact]
    public void when_image_count_and_page_visited_are_positive_then_they_are_passed_through_unchanged()
    {
        var progress = SearchCategoryProgressFactory.Create(42, 2);

        progress.ShouldBe(new SearchCategoryProgress(42, 2));
    }

    [Fact]
    public void when_image_count_is_negative_then_it_is_normalized_to_zero()
    {
        var progress = SearchCategoryProgressFactory.Create(-1, 2);

        progress.ShouldBe(new SearchCategoryProgress(0, 2));
    }

    [Fact]
    public void when_page_visited_is_negative_then_it_is_normalized_to_zero()
    {
        var progress = SearchCategoryProgressFactory.Create(42, -1);

        progress.ShouldBe(new SearchCategoryProgress(42, 0));
    }
}
