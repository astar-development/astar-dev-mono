using System.Net;
using System.Text;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Jobs;

public sealed class GivenAnHttpDownloader
{
    private const string DownloadUrl = "https://example.com/file.txt";
    private const string FileContent = "hello world";
    private const string LocalPath = "/tmp/downloaded-file.txt";
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

    private static HttpDownloader CreateSut(IHttpClientFactory factory, MockFileSystem fileSystem) => new(factory, fileSystem, Substitute.For<ILogger<HttpDownloader>>(), System.TimeProvider.System);

    [Fact]
    public void when_constructed_then_service_implements_ihttp_downloader() =>
        CreateSut(Substitute.For<IHttpClientFactory>(), new MockFileSystem()).ShouldBeAssignableTo<IHttpDownloader>();

    [Fact]
    public async Task when_download_async_is_called_with_pre_cancelled_token_then_operation_is_cancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var sut = CreateSut(CreateOkFactory(), new MockFileSystem());

        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: cts.Token));
    }

    [Fact]
    public async Task when_download_async_succeeds_then_file_is_written_with_correct_content()
    {
        var mockFileSystem = new MockFileSystem();
        var sut = CreateSut(CreateOkFactory(), mockFileSystem);

        await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        mockFileSystem.File.Exists(LocalPath).ShouldBeTrue();
        string writtenContent = mockFileSystem.File.ReadAllText(LocalPath);
        writtenContent.ShouldBe(FileContent);
    }

    [Fact]
    public async Task when_download_async_succeeds_then_file_last_write_time_matches_remote_modified()
    {
        var mockFileSystem = new MockFileSystem();
        var sut = CreateSut(CreateOkFactory(), mockFileSystem);

        await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        mockFileSystem.File.GetLastWriteTimeUtc(LocalPath).ShouldBe(RemoteModified.UtcDateTime);
    }

    private static IHttpClientFactory CreateAlways429Factory()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler(_ => response)));

        return factory;
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
