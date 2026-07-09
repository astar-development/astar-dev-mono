using System.Net;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenTheImageRetriever
{
    private const string ImageUrl = "https://example.test/images/12345.jpg";

    [Fact]
    public async Task when_the_response_is_successful_then_the_image_bytes_are_returned()
    {
        byte[] expectedBytes = [1, 2, 3, 4,];
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(expectedBytes), });
        var sut = new ImageRetriever(new HttpClient(handler));

        var bytes = (await sut.GetImageAsync(ImageUrl, TestContext.Current.CancellationToken)).ShouldBeOfType<Ok<byte[], ScrapeError>>().Value;

        bytes.ShouldBe(expectedBytes);
    }

    [Fact]
    public async Task when_the_response_is_not_successful_then_an_image_download_failed_error_is_returned()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new ImageRetriever(new HttpClient(handler));

        var error = (await sut.GetImageAsync(ImageUrl, TestContext.Current.CancellationToken)).ShouldBeOfType<Fail<byte[], ScrapeError>>().Error.ShouldBeOfType<ImageDownloadFailed>();

        error.ImageUrl.ShouldBe(ImageUrl);
    }

    [Fact]
    public async Task when_the_http_client_throws_a_request_exception_then_an_image_download_failed_error_is_returned()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));
        var sut = new ImageRetriever(new HttpClient(handler));

        var error = (await sut.GetImageAsync(ImageUrl, TestContext.Current.CancellationToken)).ShouldBeOfType<Fail<byte[], ScrapeError>>().Error.ShouldBeOfType<ImageDownloadFailed>();

        error.ImageUrl.ShouldBe(ImageUrl);
    }
}

internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(responder(request));
}
