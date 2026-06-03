using System.Net;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.TestHelpers;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Jobs;

public sealed class GivenAnHttpDownloaderCancellationLogging
{
    private const string DownloadUrl = "https://example.com/file.txt";
    private const string LocalPath = "/tmp/downloaded-file.txt";
    private static readonly DateTimeOffset RemoteModified = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task when_ct_cancelled_during_429_backoff_then_warning_logged_with_event_id_2706()
    {
        using var cts = new CancellationTokenSource();
        var logger = new TestLogger<HttpDownloader>();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new CancellingOn429Handler(cts)));
        var sut = new HttpDownloader(factory, new MockFileSystem(), logger);

        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: cts.Token));

        logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.EventId.Id == 2706);
    }

    private sealed class CancellingOn429Handler(CancellationTokenSource cts) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cts.Cancel();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        }
    }
}
