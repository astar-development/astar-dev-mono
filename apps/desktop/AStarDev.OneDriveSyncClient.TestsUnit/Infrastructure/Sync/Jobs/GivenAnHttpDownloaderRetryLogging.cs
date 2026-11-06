using System.Net;
using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.TestsUnit.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Jobs;

public sealed class GivenAnHttpDownloaderRetryLogging
{
    private const string DownloadUrl = "https://example.com/file.txt";
    private const string LocalPath = "/tmp/downloaded-file.txt";
    private static readonly DateTimeOffset RemoteModified = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider timeProvider = new();
    private readonly TestLogger<HttpDownloader> logger = new();

    [Fact]
    public async Task when_every_attempt_is_rate_limited_then_each_retry_is_debug_and_one_warning_reports_the_retries_are_exhausted()
    {
        var result = await DownloadAsync(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        result.ShouldBeOfType<Fail<System.Reactive.Unit, string>>();
        logger.Entries.Where(entry => entry.Level >= LogLevel.Warning).ShouldHaveSingleItem().EventId.Name.ShouldBe("DownloadRetriesExhausted");
        logger.Entries.Where(entry => entry.EventId.Name == "DownloadThrottled").ShouldAllBe(entry => entry.Level == LogLevel.Debug);
    }

    [Fact]
    public async Task when_every_attempt_hits_an_io_error_then_each_retry_is_debug_and_one_warning_reports_the_retries_are_exhausted()
    {
        var result = await DownloadAsync(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new IoThrowingContent() });

        result.ShouldBeOfType<Fail<System.Reactive.Unit, string>>();
        logger.Entries.Where(entry => entry.Level >= LogLevel.Warning).ShouldHaveSingleItem().EventId.Name.ShouldBe("DownloadRetriesExhausted");
        logger.Entries.Where(entry => entry.EventId.Name == "DownloadNetworkError").ShouldAllBe(entry => entry.Level == LogLevel.Debug);
    }

    private async Task<Result<System.Reactive.Unit, string>> DownloadAsync(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(new FakeHttpMessageHandler(respond)));
        var download = new HttpDownloader(factory, new MockFileSystem(), logger, timeProvider).DownloadAsync(DownloadUrl, LocalPath, RemoteModified, cancellationToken: TestContext.Current.CancellationToken);

        while (!download.IsCompleted)
        {
            timeProvider.Advance(TimeSpan.FromMinutes(5));
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        return await download;
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }

    private sealed class IoThrowingContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context) => throw new IOException("the connection was reset");

        protected override bool TryComputeLength(out long length)
        {
            length = 0;

            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() => throw new IOException("the connection was reset");
    }
}
