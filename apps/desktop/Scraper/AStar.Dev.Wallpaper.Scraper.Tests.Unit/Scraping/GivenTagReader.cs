using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenTagReader
{
    [Fact]
    public async Task when_the_page_has_tag_elements_then_their_text_and_category_are_read_in_order()
    {
        var page = CreatePageWithTagElements(("Nature", "outdoors"), ("Emma Stone", "people > actress"));
        var sut = new TagReader();

        var tags = await sut.ReadAsync(page, TestContext.Current.CancellationToken);

        tags.ShouldBe([new TagData("Nature", "outdoors"), new TagData("Emma Stone", "people > actress")]);
    }

    [Fact]
    public async Task when_a_tag_element_has_no_category_attribute_then_its_category_is_null()
    {
        var page = CreatePageWithTagElements(("Untagged", null));
        var sut = new TagReader();

        var tags = await sut.ReadAsync(page, TestContext.Current.CancellationToken);

        tags.ShouldBe([new TagData("Untagged", null)]);
    }

    [Fact]
    public async Task when_the_page_has_no_tag_elements_then_an_empty_list_is_returned()
    {
        var page = CreatePageWithTagElements();
        var sut = new TagReader();

        var tags = await sut.ReadAsync(page, TestContext.Current.CancellationToken);

        tags.ShouldBeEmpty();
    }

    private static IPage CreatePageWithTagElements(params (string Text, string? Category)[] tagElements)
    {
        var page = Substitute.For<IPage>();
        var locators = tagElements.Select(tag =>
        {
            var locator = Substitute.For<ILocator>();
            locator.InnerTextAsync().Returns(Task.FromResult(tag.Text));
            locator.GetAttributeAsync("original-title").Returns(Task.FromResult(tag.Category));

            return locator;
        }).ToList();

        var container = Substitute.For<ILocator>();
        container.AllAsync().Returns(Task.FromResult<IReadOnlyList<ILocator>>(locators));
        page.Locator(".tagname").Returns(container);

        return page;
    }
}
