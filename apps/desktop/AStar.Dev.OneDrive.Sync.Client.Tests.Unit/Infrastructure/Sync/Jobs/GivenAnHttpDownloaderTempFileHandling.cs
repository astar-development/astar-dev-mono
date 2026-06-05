using System.IO.Abstractions;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.TestHelpers;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Jobs;

public sealed class GivenAnHttpDownloaderTempFileHandling
{
    private const string DownloadUrl = "https://example.com/file.jpg";
    private const string LocalPath = "/tmp/downloaded-file.jpg";
    private const string TempPath = LocalPath + ".download";
    private static readonly DateTimeOffset RemoteModified = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private static IHttpClientFactory CreateOkFactory(string content = "file content")
    {
        var factory = Substitute.For<IHttpClientFactory>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8)
        };
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new FakeHttpMessageHandler(_ => response)));

        return factory;
    }

    private static IFileSystem CreateAlwaysFailMoveFileSystem()
    {
        var mockFs = new MockFileSystem();
        var fakeFs = Substitute.For<IFileSystem>();
        fakeFs.FileStream.Returns(mockFs.FileStream);
        fakeFs.Directory.Returns(mockFs.Directory);
        fakeFs.Path.Returns(mockFs.Path);
        fakeFs.File.When(f => f.Move(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()))
            .Do(_ => throw new IOException("The process cannot access the file because it is being used by another process."));

        return fakeFs;
    }

    private static IFileSystem CreateCapturingFileSystem(string[] capturedTempPath)
    {
        var mockFs = new MockFileSystem();
        var fakeFs = Substitute.For<IFileSystem>();
        fakeFs.FileStream.Returns(mockFs.FileStream);
        fakeFs.Directory.Returns(mockFs.Directory);
        fakeFs.Path.Returns(mockFs.Path);
        fakeFs.File.When(f => f.Move(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()))
            .Do(callInfo => capturedTempPath[0] = callInfo.ArgAt<string>(0));

        return fakeFs;
    }

    [Fact]
    public async Task when_download_succeeds_then_no_temp_file_remains()
    {
        var mockFileSystem = new MockFileSystem();
        var sut = new HttpDownloader(CreateOkFactory(), mockFileSystem, Substitute.For<ILogger<HttpDownloader>>());

        await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        mockFileSystem.File.Exists(TempPath).ShouldBeFalse();
    }

    [Fact]
    public async Task when_download_succeeds_then_content_is_written_to_final_path_not_temp_path()
    {
        const string content = "expected file content";
        var mockFileSystem = new MockFileSystem();
        var sut = new HttpDownloader(CreateOkFactory(content), mockFileSystem, Substitute.For<ILogger<HttpDownloader>>());

        await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        mockFileSystem.File.Exists(LocalPath).ShouldBeTrue();
        mockFileSystem.File.ReadAllText(LocalPath).ShouldBe(content);
    }

    [Fact]
    public async Task when_file_move_fails_all_retries_then_result_is_error()
    {
        var sut = new HttpDownloader(CreateOkFactory(), CreateAlwaysFailMoveFileSystem(), Substitute.For<ILogger<HttpDownloader>>());

        var result = await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<System.Reactive.Unit, string>.Error>();
    }

    [Fact]
    public async Task when_file_move_fails_all_retries_then_temp_file_is_deleted()
    {
        var fakeFs = CreateAlwaysFailMoveFileSystem();
        var sut = new HttpDownloader(CreateOkFactory(), fakeFs, Substitute.For<ILogger<HttpDownloader>>());

        await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        fakeFs.File.Received(1).Delete(Arg.Is<string>(p => p.EndsWith(".download")));
    }

    [Fact]
    public async Task when_file_move_fails_then_warning_is_logged_per_failed_retry_attempt()
    {
        var logger = new TestLogger<HttpDownloader>();
        var sut = new HttpDownloader(CreateOkFactory(), CreateAlwaysFailMoveFileSystem(), logger);

        await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        logger.Entries.Count(e => e.Level == LogLevel.Warning && e.EventId.Id == 2707).ShouldBe(2);
    }

    [Fact]
    public async Task when_all_move_retries_exhausted_then_error_is_logged_with_event_2708()
    {
        var logger = new TestLogger<HttpDownloader>();
        var sut = new HttpDownloader(CreateOkFactory(), CreateAlwaysFailMoveFileSystem(), logger);

        await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        logger.Entries.ShouldContain(e => e.Level == LogLevel.Error && e.EventId.Id == 2708);
    }

    [Fact]
    public async Task when_download_succeeds_then_temp_path_is_not_plain_dot_download_suffix()
    {
        string[] capturedTempPath = [string.Empty];
        var sut = new HttpDownloader(CreateOkFactory(), CreateCapturingFileSystem(capturedTempPath), Substitute.For<ILogger<HttpDownloader>>());

        await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        capturedTempPath[0].ShouldNotBe(LocalPath + ".download");
    }

    [Fact]
    public async Task when_download_succeeds_then_temp_path_contains_guid_segment_before_download_extension()
    {
        string[] capturedTempPath = [string.Empty];
        var sut = new HttpDownloader(CreateOkFactory(), CreateCapturingFileSystem(capturedTempPath), Substitute.For<ILogger<HttpDownloader>>());

        await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        capturedTempPath[0].ShouldEndWith(".download");
        string[] segments = capturedTempPath[0].Split('.');
        string guidSegment = segments[^2];
        Regex.IsMatch(guidSegment, "^[0-9a-f]{32}$").ShouldBeTrue();
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
