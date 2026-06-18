using System.Net;
using System.Text;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Jobs;

public sealed class GivenAnHttpDownloaderIoExceptionRetry
{
    private const string DownloadUrl = "https://example.com/file.txt";
    private const string LocalPath = "/tmp/downloaded-file.txt";
    private static readonly DateTimeOffset RemoteModified = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task when_io_exception_thrown_reading_body_once_then_download_is_retried_and_succeeds()
    {
        int callCount = 0;
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ =>
            new HttpClient(new FakeHttpMessageHandler(_ =>
            {
                callCount++;
                return callCount == 1
                    ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new IoThrowingContent() }
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("hello", Encoding.UTF8) };
            })));

        var sut = new HttpDownloader(factory, new MockFileSystem(), Substitute.For<ILogger<HttpDownloader>>());

        var result = await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<System.Reactive.Unit, string>.Ok>();
        callCount.ShouldBe(2);
    }

    [Fact]
    public async Task when_ct_cancelled_during_io_exception_backoff_then_operation_cancelled_exception_propagates()
    {
        using var cts = new CancellationTokenSource();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ =>
            new HttpClient(new CancellingIoHandler(cts)));

        var sut = new HttpDownloader(factory, new MockFileSystem(), Substitute.For<ILogger<HttpDownloader>>());

        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: cts.Token));
    }

    [Fact]
    public async Task when_io_exception_occurs_on_first_attempt_then_temp_file_deleted_before_retry()
    {
        int callCount = 0;
        var mockFileSystem = new MockFileSystem();
        var deletedPaths = new List<string>();

        var spyFile = Substitute.For<System.IO.Abstractions.IFile>();
        spyFile.Exists(Arg.Any<string>()).Returns(false);
        spyFile.When(f => f.Delete(Arg.Any<string>()))
            .Do(callInfo => deletedPaths.Add(callInfo.ArgAt<string>(0)));
        spyFile.When(f => f.Move(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()))
            .Do(callInfo => mockFileSystem.File.Move(callInfo.ArgAt<string>(0), callInfo.ArgAt<string>(1), callInfo.ArgAt<bool>(2)));
        spyFile.When(f => f.SetLastWriteTimeUtc(Arg.Any<string>(), Arg.Any<DateTime>()))
            .Do(x => mockFileSystem.File.SetLastWriteTimeUtc(x.ArgAt<string>(0), x.ArgAt<DateTime>(1)));

        var spyFs = Substitute.For<System.IO.Abstractions.IFileSystem>();
        spyFs.File.Returns(spyFile);
        spyFs.FileStream.Returns(mockFileSystem.FileStream);
        spyFs.Directory.Returns(mockFileSystem.Directory);
        spyFs.Path.Returns(mockFileSystem.Path);

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ =>
            new HttpClient(new FakeHttpMessageHandler(_ =>
            {
                callCount++;
                return callCount == 1
                    ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new IoThrowingContent() }
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("data", Encoding.UTF8) };
            })));

        var sut = new HttpDownloader(factory, spyFs, Substitute.For<ILogger<HttpDownloader>>());

        await sut.DownloadAsync(DownloadUrl, LocalPath, RemoteModified, ct: TestContext.Current.CancellationToken);

        deletedPaths.ShouldContain(path => path.StartsWith(LocalPath + ".") && path.EndsWith(".download"));
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private sealed class CancellingIoHandler(CancellationTokenSource cts) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cts.Cancel();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new IoThrowingContent() });
        }
    }

    private sealed class IoThrowingContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            Task.FromException(new IOException("Connection reset by peer"));

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(new IoThrowingStream());
    }

    private sealed class IoThrowingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("Connection reset by peer");
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => Task.FromException<int>(new IOException("Connection reset by peer"));
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => ValueTask.FromException<int>(new IOException("Connection reset by peer"));
    }
}
