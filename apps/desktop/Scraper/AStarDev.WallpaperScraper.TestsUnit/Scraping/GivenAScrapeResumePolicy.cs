using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.WallpaperScraper.TestsUnit.Scraping;

public sealed class GivenAScrapeResumePolicy
{
    private readonly ScrapeResumePolicy policy = new(new ScrapeLimits(3, 4));

    [Fact]
    public void when_there_is_no_previous_progress_then_paging_starts_at_the_first_page()
        => policy.ResumePage(Option.None<SearchCategoryProgress>()).ShouldBe(1);

    [Fact]
    public void when_the_previous_scrape_stopped_part_way_then_paging_resumes_after_the_last_page_visited()
        => policy.ResumePage(Option.Some(new SearchCategoryProgress(50, 2, 10))).ShouldBe(3);

    [Fact]
    public void when_the_previous_scrape_visited_no_pages_then_paging_starts_at_the_first_page()
        => policy.ResumePage(Option.Some(new SearchCategoryProgress(50, 0, 10))).ShouldBe(1);

    [Fact]
    public void when_the_previous_scrape_completed_all_pages_then_paging_starts_at_the_first_page()
        => policy.ResumePage(Option.Some(new SearchCategoryProgress(50, 2, 2))).ShouldBe(1);

    [Fact]
    public void when_the_previous_scrape_reached_the_page_limit_then_paging_starts_at_the_first_page()
        => policy.ResumePage(Option.Some(new SearchCategoryProgress(50, 4, 10))).ShouldBe(1);

    [Fact]
    public void when_there_is_no_previous_progress_then_the_category_is_not_the_same_as_the_previous_scrape()
        => ScrapeResumePolicy.IsSameCategoryAsPreviousScrape(Option.None<SearchCategoryProgress>(), new Meta(10, 50)).ShouldBeFalse();

    [Theory]
    [InlineData(50, 10, true)]
    [InlineData(51, 10, false)]
    [InlineData(50, 11, false)]
    public void when_previous_progress_exists_then_the_category_is_the_same_only_when_the_total_and_last_page_match(int total, int lastPage, bool expected)
        => ScrapeResumePolicy.IsSameCategoryAsPreviousScrape(Option.Some(new SearchCategoryProgress(50, 2, 10)), new Meta(lastPage, total)).ShouldBe(expected);

    [Fact]
    public void when_there_is_no_previous_progress_then_the_scrape_is_not_unchanged()
        => policy.IsUnchangedSincePreviousScrape(Option.None<SearchCategoryProgress>(), new Meta(2, 50)).ShouldBeFalse();

    [Fact]
    public void when_the_same_category_was_completely_scraped_then_it_is_unchanged()
        => policy.IsUnchangedSincePreviousScrape(Option.Some(new SearchCategoryProgress(50, 2, 2)), new Meta(2, 50)).ShouldBeTrue();

    [Fact]
    public void when_the_same_category_was_only_partly_scraped_then_it_is_changed()
        => policy.IsUnchangedSincePreviousScrape(Option.Some(new SearchCategoryProgress(50, 1, 2)), new Meta(2, 50)).ShouldBeFalse();

    [Fact]
    public void when_the_category_count_changed_then_it_is_changed_even_if_completely_scraped()
        => policy.IsUnchangedSincePreviousScrape(Option.Some(new SearchCategoryProgress(50, 2, 2)), new Meta(2, 60)).ShouldBeFalse();

    [Theory]
    [InlineData(1, 2, false)]
    [InlineData(2, 2, true)]
    [InlineData(4, 10, true)]
    [InlineData(3, 10, false)]
    public void when_a_page_is_visited_then_it_is_last_only_at_the_reported_last_page_or_the_page_limit(int pageNumber, int lastPage, bool expected)
        => policy.IsLastPageToVisit(pageNumber, 1, new Meta(lastPage)).ShouldBe(expected);

    [Theory]
    [InlineData(1, 1, false)]
    [InlineData(1, 2, false)]
    [InlineData(1, 3, true)]
    [InlineData(6, 7, false)]
    [InlineData(6, 8, true)]
    public void when_a_page_is_visited_then_it_is_last_once_the_pages_allowed_per_run_have_been_visited_from_the_start_page(int startPage, int pageNumber, bool expected)
        => new ScrapeResumePolicy(new ScrapeLimits(int.MaxValue, int.MaxValue, 3)).IsLastPageToVisit(pageNumber, startPage, new Meta(50)).ShouldBe(expected);
}
