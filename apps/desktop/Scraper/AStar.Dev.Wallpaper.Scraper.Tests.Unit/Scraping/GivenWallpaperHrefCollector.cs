using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperHrefCollector
{
    private readonly IOptions<ScrapeConfiguration> scrapeConfiguration = Options.Create(new ScrapeConfiguration
    {
        SearchConfiguration = new SearchConfiguration
        {
            BaseUrl = new Uri("https://example.com/")
        }
    });

    [Fact]
    public async Task when_previews_link_to_wallpaper_pages_then_their_hrefs_are_collected_in_order()
    {
        var page = CreatePageWithPreviewHrefs("https://example.com/w/abc123", "https://example.com/w/def456");
        var sut = new WallpaperHrefCollector(scrapeConfiguration);

        var hrefs = await sut.CollectAsync(page, TestContext.Current.CancellationToken);

        hrefs.ShouldBe(["https://example.com/w/abc123", "https://example.com/w/def456"]);
    }

    [Fact]
    public async Task when_a_preview_link_does_not_point_at_a_wallpaper_page_then_it_is_excluded()
    {
        var page = CreatePageWithPreviewHrefs("https://example.com/w/abc123", "https://example.com/other/page");

        var sut = new WallpaperHrefCollector(scrapeConfiguration);

        var hrefs = await sut.CollectAsync(page, TestContext.Current.CancellationToken);

        hrefs.ShouldBe(["https://example.com/w/abc123"]);
    }

    [Fact]
    public async Task when_a_preview_link_has_no_href_then_it_is_excluded()
    {
        var page = CreatePageWithPreviewHrefs(null, "https://example.com/w/def456");

        var sut = new WallpaperHrefCollector(scrapeConfiguration);

        var hrefs = await sut.CollectAsync(page, TestContext.Current.CancellationToken);

        hrefs.ShouldBe(["https://example.com/w/def456"]);
    }

    [Fact]
    public async Task when_a_wallpaper_href_differs_only_in_case_then_it_is_still_matched()
    {
        var page = CreatePageWithPreviewHrefs("HTTPS://example.com/W/abc123");

        var sut = new WallpaperHrefCollector(scrapeConfiguration);

        var hrefs = await sut.CollectAsync(page, TestContext.Current.CancellationToken);

        hrefs.ShouldBe(["HTTPS://example.com/W/abc123"]);
    }

    private static IPage CreatePageWithPreviewHrefs(params string?[] hrefs)
    {
        var page = Substitute.For<IPage>();
        var previews = hrefs.Select(href =>
        {
            var preview = Substitute.For<ILocator>();
            preview.GetAttributeAsync("href").Returns(Task.FromResult(href));

            return preview;
        }).ToList();

        var container = Substitute.For<ILocator>();
        container.AllAsync().Returns(Task.FromResult<IReadOnlyList<ILocator>>(previews));
        page.GetByRole(AriaRole.Link).Returns(container);

        return page;
    }
}
