using System.IO.Abstractions;
using System.Net;
using System.Text;
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

    [Fact]
    public async Task when_download_succeeds_then_no_temp_file_remains()
    {
        var mockFileSystem = new MockFileSystem();
        var sut = new HttpDownloader(CreateOkFactory(), mockFileSystem, Substitute.For<ILogger<HttpDownloader>>());

        await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        mockFileSystem.Directory.GetFiles(mockFileSystem.Path.GetDirectoryName(LocalPath)!)
            .ShouldNotContain(f => f.EndsWith(".download"));
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
    public async Task when_two_concurrent_downloads_of_same_file_then_distinct_temp_paths_are_used()
    {
        var writtenPaths = new System.Collections.Concurrent.ConcurrentBag<string>();
        var mockFs = new MockFileSystem();

        var spyFileStreamFactory = Substitute.For<IFileStreamFactory>();
        spyFileStreamFactory
            .New(Arg.Any<string>(), Arg.Any<FileMode>(), Arg.Any<FileAccess>(), Arg.Any<FileShare>(), Arg.Any<int>(), Arg.Any<bool>())
            .Returns(callInfo =>
            {
                var path = callInfo.ArgAt<string>(0);
                writtenPaths.Add(path);
                return mockFs.FileStream.New(path, callInfo.ArgAt<FileMode>(1), callInfo.ArgAt<FileAccess>(2), callInfo.ArgAt<FileShare>(3), callInfo.ArgAt<int>(4), callInfo.ArgAt<bool>(5));
            });

        var spyFs = Substitute.For<IFileSystem>();
        spyFs.FileStream.Returns(spyFileStreamFactory);
        spyFs.File.Returns(mockFs.File);
        spyFs.Directory.Returns(mockFs.Directory);
        spyFs.Path.Returns(mockFs.Path);

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ =>
            new HttpClient(new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("content", Encoding.UTF8) })));

        var sut = new HttpDownloader(factory, spyFs, Substitute.For<ILogger<HttpDownloader>>());

        await Task.WhenAll(
            sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken),
            sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken));

        writtenPaths.Distinct().Count().ShouldBe(2);
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

        fakeFs.File.Received(1).Delete(Arg.Is<string>(p => p.StartsWith(LocalPath + ".") && p.EndsWith(".download")));
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

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
