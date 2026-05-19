using System.Net;
using System.Text;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

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

    private static HttpDownloader CreateSut(IHttpClientFactory factory, MockFileSystem fileSystem) => new(factory, fileSystem, Substitute.For<ILogger<HttpDownloader>>());

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

    [Fact]
    public async Task when_download_async_succeeds_with_progress_then_progress_is_reported()
    {
        var mockFileSystem = new MockFileSystem();
        var reportedValues = new List<long>();
        var progress = new Progress<long>(reportedValues.Add);
        var sut = CreateSut(CreateOkFactory(), mockFileSystem);

        await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, progress, TestContext.Current.CancellationToken);

        await Task.Delay(50, TestContext.Current.CancellationToken);

        reportedValues.ShouldNotBeEmpty();
        reportedValues[^1].ShouldBe(Encoding.UTF8.GetByteCount(FileContent));
    }

    [Fact]
    public async Task when_download_is_rate_limited_beyond_max_retries_then_result_is_error()
    {
        var factory = CreateAlways429Factory();
        var sut = CreateSut(factory, new MockFileSystem());

        var result = await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        var error = result.ShouldBeAssignableTo<Result<System.Reactive.Unit, string>.Error>();
        error!.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    private static IHttpClientFactory CreateAlways429Factory()
    {
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
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
