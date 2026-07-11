using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Support;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Services;

public sealed class GivenAnImageDownloader
{
    private const string ImageUrl = "https://example.test/images/12345.data";

    [Fact]
    public async Task when_the_retriever_succeeds_first_time_then_the_bytes_are_returned_without_a_retry()
    {
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success<byte[], ScrapeError>([1, 2, 3,])));
        var delayStrategy = Substitute.For<IDelayStrategy>();
        var sut = new ImageDownloader(imageRetriever, delayStrategy);

        var result = await sut.DownloadAsync(ImageUrl, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<byte[], ScrapeError>>().Value.ShouldBe([1, 2, 3,]);
        await imageRetriever.Received(1).GetImageAsync(ImageUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_retriever_always_fails_then_exactly_one_retry_with_a_retry_delay_is_made()
    {
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(Result.Failure<byte[], ScrapeError>(ScrapeErrorFactory.CreateImageDownloadFailed(ImageUrl, "download failed"))));
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var sut = new ImageDownloader(imageRetriever, delayStrategy);

        var result = await sut.DownloadAsync(ImageUrl, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Fail<byte[], ScrapeError>>().Error.ShouldBeOfType<ImageDownloadFailed>();
        await imageRetriever.Received(2).GetImageAsync(ImageUrl, Arg.Any<CancellationToken>());
        await delayStrategy.Received(1).DelayAsync(DelayKind.Retry, Arg.Any<CancellationToken>());
    }
}
