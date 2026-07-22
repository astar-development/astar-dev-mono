using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperImageDownloader
{
    [Fact]
    public async Task when_the_image_request_succeeds_then_the_response_body_bytes_are_returned()
    {
        byte[] imageBytes = [1, 2, 3, 4];
        var page = CreatePageWithApiResponse(true, imageBytes);
        var sut = new WallpaperImageDownloader();

        var result = await sut.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", "Nature", ["forest"], TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<byte[]>>();
        result.ShouldBe(new Success<byte[]>(imageBytes));
    }

    [Fact]
    public async Task when_the_image_request_returns_a_non_success_status_then_a_failure_result_is_returned()
    {
        var page = CreatePageWithApiResponse(false, []);
        var sut = new WallpaperImageDownloader();

        var result = await sut.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", "Nature", ["forest"], TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Failure<byte[]>>();
    }

    private static IPage CreatePageWithApiResponse(bool ok, byte[] bodyBytes)
    {
        var apiResponse = Substitute.For<IAPIResponse>();
        apiResponse.Ok.Returns(ok);
        apiResponse.Status.Returns(ok ? 200 : 404);
        apiResponse.BodyAsync().Returns(Task.FromResult(bodyBytes));

        var apiRequestContext = Substitute.For<IAPIRequestContext>();
        apiRequestContext.GetAsync("https://wallhaven.cc/images/pic.jpg", Arg.Any<APIRequestContextOptions>()).Returns(Task.FromResult(apiResponse));

        var page = Substitute.For<IPage>();
        page.APIRequest.Returns(apiRequestContext);

        return page;
    }
}
