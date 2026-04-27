using System.Net;
using System.Net.Http;
using System.Text;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenAnHttpDownloader
{
    private const string DownloadUrl   = "https://example.com/file.txt";
    private const string FileContent   = "hello world";
    private static readonly DateTimeOffset RemoteModified = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private static IHttpClientFactory CreateFactoryReturning(HttpResponseMessage response)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler(_ => response)));

        return factory;
    }

    private static IHttpClientFactory CreateOkFactory(string content = FileContent)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8)
        };

        return CreateFactoryReturning(response);
    }

    [Fact]
    public void when_constructed_then_service_implements_ihttp_downloader() =>
        new HttpDownloader(Substitute.For<IHttpClientFactory>()).ShouldBeAssignableTo<IHttpDownloader>();

    [Fact]
    public async Task when_download_async_is_called_with_pre_cancelled_token_then_operation_is_cancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = new HttpDownloader(CreateOkFactory());

        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.DownloadAsync(DownloadUrl, "/tmp/irrelevant.txt", RemoteModified, ct: cts.Token));
    }

    [Fact]
    public async Task when_download_async_succeeds_then_file_is_written_with_correct_content()
    {
        string localPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            var sut = new HttpDownloader(CreateOkFactory());

            await sut.DownloadAsync(DownloadUrl, localPath, RemoteModified, ct: TestContext.Current.CancellationToken);

            File.Exists(localPath).ShouldBeTrue();
            string writtenContent = await File.ReadAllTextAsync(localPath, TestContext.Current.CancellationToken);
            writtenContent.ShouldBe(FileContent);
        }
        finally
        {
            if(File.Exists(localPath))
                File.Delete(localPath);
        }
    }

    [Fact]
    public async Task when_download_async_succeeds_then_file_last_write_time_matches_remote_modified()
    {
        string localPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            var sut = new HttpDownloader(CreateOkFactory());

            await sut.DownloadAsync(DownloadUrl, localPath, RemoteModified, ct: TestContext.Current.CancellationToken);

            File.GetLastWriteTimeUtc(localPath).ShouldBe(RemoteModified.UtcDateTime);
        }
        finally
        {
            if(File.Exists(localPath))
                File.Delete(localPath);
        }
    }

    [Fact]
    public async Task when_download_async_succeeds_with_progress_then_progress_is_reported()
    {
        string localPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var reportedValues = new List<long>();

        try
        {
            var progress = new Progress<long>(reportedValues.Add);
            var sut = new HttpDownloader(CreateOkFactory());

            await sut.DownloadAsync(DownloadUrl, localPath, RemoteModified, progress, TestContext.Current.CancellationToken);

            await Task.Delay(50, TestContext.Current.CancellationToken);

            reportedValues.ShouldNotBeEmpty();
            reportedValues[^1].ShouldBe(Encoding.UTF8.GetByteCount(FileContent));
        }
        finally
        {
            if(File.Exists(localPath))
                File.Delete(localPath);
        }
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
