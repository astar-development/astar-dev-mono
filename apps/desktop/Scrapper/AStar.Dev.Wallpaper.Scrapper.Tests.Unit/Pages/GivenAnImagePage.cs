using NSubstitute.ExceptionExtensions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.DTOs;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Pages;

public sealed class GivenAnImagePage
{
    private const string Link = "https://example.test/w/12345";
    private const string CategoryName = "Cars";
    private const string ImageUrl = "https://example.test/images/12345.jpg";

    private static ImagePage BuildSut(IPage page, TagsToIgnoreCompletely? tagsToIgnoreCompletely = null, TagsTextToIgnore? tagsTextToIgnore = null)
    {
        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));
        var scrapeConfiguration = new ScrapeConfigurationBuilder().Build();

        return new ImagePage(playwrightService, scrapeConfiguration, tagsToIgnoreCompletely ?? new(), tagsTextToIgnore ?? new());
    }

    private static IPage BuildPage(string tagText, string? tagCategory, string? imageSrc)
    {
        var tagLocator = Substitute.For<ILocator>();
        tagLocator.InnerTextAsync().Returns(Task.FromResult(tagText));
        tagLocator.GetAttributeAsync("original-title").Returns(Task.FromResult(tagCategory));

        var tagsLocator = Substitute.For<ILocator>();
        tagsLocator.AllAsync().Returns(Task.FromResult<IReadOnlyList<ILocator>>([tagLocator,]));

        var imageLocator = Substitute.For<ILocator>();
        imageLocator.GetAttributeAsync("src").Returns(Task.FromResult(imageSrc));

        var page = Substitute.For<IPage>();
        page.Locator(".tagname", Arg.Any<PageLocatorOptions>()).Returns(tagsLocator);
        page.Locator("#wallpaper", Arg.Any<PageLocatorOptions>()).Returns(imageLocator);

        return page;
    }

    [Fact]
    public async Task when_the_tags_are_accepted_then_the_outcome_is_a_scraped_image()
    {
        var sut = BuildSut(BuildPage("Sports Car", "Vehicles > Cars & Motorcycles", ImageUrl));

        var result = await sut.GetImageFromPageAsync(Link, CategoryName);

        result.ShouldBeOfType<Ok<ImagePageOutcome, ScrapeError>>().Value.ShouldBeOfType<ScrapedImage>();
    }

    [Fact]
    public async Task when_the_tags_are_accepted_then_the_scraped_image_carries_the_image_url()
    {
        var sut = BuildSut(BuildPage("Sports Car", "Vehicles > Cars & Motorcycles", ImageUrl));

        var result = await sut.GetImageFromPageAsync(Link, CategoryName);

        result.ShouldBeOfType<Ok<ImagePageOutcome, ScrapeError>>().Value.ShouldBeOfType<ScrapedImage>().ImageUrl.ShouldBe(ImageUrl);
    }

    [Fact]
    public async Task when_the_tags_are_accepted_then_the_scraped_image_carries_the_raw_tag_data()
    {
        var sut = BuildSut(BuildPage("Sports Car", "Vehicles > Cars & Motorcycles", ImageUrl));

        var result = await sut.GetImageFromPageAsync(Link, CategoryName);

        var scraped = result.ShouldBeOfType<Ok<ImagePageOutcome, ScrapeError>>().Value.ShouldBeOfType<ScrapedImage>();
        scraped.RawTags.ShouldBe([new TagData("Sports Car", "Vehicles > Cars & Motorcycles"),]);
    }

    [Fact]
    public async Task when_a_tag_category_matches_the_ignore_completely_list_then_the_outcome_is_a_skipped_image()
    {
        var tagsToIgnoreCompletely = new TagsToIgnoreCompletely { Tags = ["ExcludedCategory",], };
        var sut = BuildSut(BuildPage("BadCategory", "ExcludedCategory", ImageUrl), tagsToIgnoreCompletely);

        var result = await sut.GetImageFromPageAsync(Link, CategoryName);

        result.ShouldBeOfType<Ok<ImagePageOutcome, ScrapeError>>().Value.ShouldBeOfType<SkippedImage>();
    }

    [Fact]
    public async Task when_a_tag_category_matches_the_ignore_completely_list_then_the_skipped_image_carries_the_raw_tag_data()
    {
        var tagsToIgnoreCompletely = new TagsToIgnoreCompletely { Tags = ["ExcludedCategory",], };
        var sut = BuildSut(BuildPage("BadCategory", "ExcludedCategory", ImageUrl), tagsToIgnoreCompletely);

        var result = await sut.GetImageFromPageAsync(Link, CategoryName);

        var skipped = result.ShouldBeOfType<Ok<ImagePageOutcome, ScrapeError>>().Value.ShouldBeOfType<SkippedImage>();
        skipped.RawTags.ShouldBe([new TagData("BadCategory", "ExcludedCategory"),]);
    }

    [Fact]
    public async Task when_navigating_to_the_page_throws_then_a_page_load_failed_error_is_returned()
    {
        var page = Substitute.For<IPage>();
        page.GotoAsync(Link, Arg.Any<PageGotoOptions>()).ThrowsAsync(new PlaywrightException("navigation failed"));
        var sut = BuildSut(page);

        var result = await sut.GetImageFromPageAsync(Link, CategoryName);

        result.ShouldBeOfType<Fail<ImagePageOutcome, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>();
    }

    [Fact]
    public async Task when_navigating_to_the_page_throws_then_the_page_load_failed_error_carries_the_link_as_the_url()
    {
        var page = Substitute.For<IPage>();
        page.GotoAsync(Link, Arg.Any<PageGotoOptions>()).ThrowsAsync(new PlaywrightException("navigation failed"));
        var sut = BuildSut(page);

        var result = await sut.GetImageFromPageAsync(Link, CategoryName);

        result.ShouldBeOfType<Fail<ImagePageOutcome, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>().Url.ShouldBe(Link);
    }
}
