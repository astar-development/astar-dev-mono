using System.Net;
using System.Text;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Sync.Jobs;

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

    private static HttpDownloader CreateSut(IHttpClientFactory factory, MockFileSystem fileSystem, System.TimeProvider? timeProvider = null) => new(factory, fileSystem, Substitute.For<ILogger<HttpDownloader>>(), timeProvider ?? System.TimeProvider.System);

    [Fact]
    public async Task when_download_is_rate_limited_beyond_max_retries_then_result_is_error()
    {
        var factory = CreateAlways429Factory();
        var timeProvider = new FakeTimeProvider();
        var sut = CreateSut(factory, new MockFileSystem(), timeProvider);

        var downloadTask = sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        while (!downloadTask.IsCompleted)
        {
            await Task.Delay(1, TestContext.Current.CancellationToken);
            timeProvider.Advance(TimeSpan.FromMinutes(5));
        }

        var result = await downloadTask;

        var error = result.ShouldBeAssignableTo<Result<System.Reactive.Unit, string>.Error>();
        error!.Reason.ShouldNotBeNullOrWhiteSpace();
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
