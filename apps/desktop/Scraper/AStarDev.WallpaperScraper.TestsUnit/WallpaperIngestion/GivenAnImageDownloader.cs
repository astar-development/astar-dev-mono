using System.Diagnostics.CodeAnalysis;
using System.Net;
using AStarDev.ControlDb.FileDetail;
using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.WallpaperScraper.WallpaperIngestion;
using Microsoft.Extensions.Time.Testing;
using Testably.Abstractions.Testing;

namespace AStarDev.WallpaperScraper.TestsUnit.WallpaperIngestion;

public sealed class GivenAnImageDownloader
{
    private readonly MockFileSystem fileSystem = new();
    private readonly ImageDownloader downloader;

    public GivenAnImageDownloader() => downloader = new(fileSystem, System.TimeProvider.System, DownloadPacing.None, ScrapeTimeouts.Default);

    [Fact]
    public async Task when_downloading_an_image_succeeds_then_it_is_written_to_the_directory_and_the_saved_path_and_progress_are_reported()
    {
        var imageBytes = new byte[] { 1, 2, 3, 4, 5 };
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(imageBytes) });
        var progress = new CapturingProgress();
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var expectedPath = fileSystem.Path.Combine(directory, "wallpaper-3.jpg");
        var wallpaper = CreateWallpaper(id: "wallpaper-3", path: "https://example.test/image.jpg");

        var savedPath = await downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Cars"), progress, client, CancellationToken.None);

        savedPath.ShouldBe(expectedPath);
        (await fileSystem.File.ReadAllBytesAsync(expectedPath, TestContext.Current.CancellationToken)).ShouldBe(imageBytes);
        progress.Messages.ShouldContain("Downloading image for wallpaper wallpaper-3 from https://example.test/image.jpg");
    }

    [Fact]
    public async Task when_downloading_a_non_jpg_image_then_it_is_written_with_the_supplied_extension()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([9, 9, 9]) });
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var wallpaper = CreateWallpaper(id: "wallpaper-8", path: "https://example.test/image.png");

        _ = await downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".png"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None);

        (fileSystem.File.Exists(fileSystem.Path.Combine(directory, "wallpaper-8.png")), fileSystem.File.Exists(fileSystem.Path.Combine(directory, "wallpaper-8.jpg"))).ShouldBe((true, false));
    }

    [Fact]
    public async Task when_the_request_has_a_prefixed_file_name_then_the_file_is_saved_under_it()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });
        var wallpaper = CreateWallpaper(id: "abc123", path: "https://example.test/full/abc123.jpg");
        var request = new WallpaperFileRequest(wallpaper, "some-directory", new FileName("Max_Verstappen_abc123.jpg"), "Top Wallpapers");

        _ = await downloader.DownloadAsync(request, new CapturingProgress(), client, CancellationToken.None);

        fileSystem.File.Exists(fileSystem.Path.Combine("some-directory", "Max_Verstappen_abc123.jpg")).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_target_directory_does_not_exist_yet_then_it_is_created_before_writing_the_file()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });
        var directory = fileSystem.Path.Combine("root-directory", "my-category");
        fileSystem.Directory.Exists(directory).ShouldBeFalse();
        var wallpaper = CreateWallpaper(id: "wallpaper-6", path: "https://example.test/image.jpg");

        _ = await downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None);

        (fileSystem.Directory.Exists(directory), fileSystem.File.Exists(fileSystem.Path.Combine(directory, "wallpaper-6.jpg"))).ShouldBe((true, true));
    }

    [Fact]
    public async Task when_downloading_an_image_receives_a_non_success_status_then_it_throws_and_writes_no_file()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var wallpaper = CreateWallpaper(id: "wallpaper-4", path: "https://example.test/image.jpg");

        await Should.ThrowAsync<HttpRequestException>(
            () => downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None));

        fileSystem.File.Exists(fileSystem.Path.Combine(directory, "wallpaper-4.jpg")).ShouldBeFalse();
    }

    [Fact]
    public async Task when_reading_the_image_fails_part_way_then_no_partial_file_is_left_behind()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new FailingStream()) });
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var wallpaper = CreateWallpaper(id: "wallpaper-9", path: "https://example.test/image.jpg");

        await Should.ThrowAsync<IOException>(
            () => downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None));

        fileSystem.Directory.GetFiles(directory).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_reading_the_image_fails_part_way_then_an_existing_file_of_the_same_name_is_left_untouched()
    {
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var existingPath = fileSystem.Path.Combine(directory, "wallpaper-10.jpg");
        fileSystem.Directory.CreateDirectory(directory);
        await fileSystem.File.WriteAllBytesAsync(existingPath, [7, 7, 7], TestContext.Current.CancellationToken);
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new FailingStream()) });
        var wallpaper = CreateWallpaper(id: "wallpaper-10", path: "https://example.test/image.jpg");

        await Should.ThrowAsync<IOException>(
            () => downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None));

        (await fileSystem.File.ReadAllBytesAsync(existingPath, TestContext.Current.CancellationToken)).ShouldBe([7, 7, 7]);
    }

    [Fact]
    public async Task when_the_download_succeeds_then_only_the_final_file_is_left_and_it_replaces_an_existing_one()
    {
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var finalPath = fileSystem.Path.Combine(directory, "wallpaper-11.jpg");
        fileSystem.Directory.CreateDirectory(directory);
        await fileSystem.File.WriteAllBytesAsync(finalPath, [7, 7, 7], TestContext.Current.CancellationToken);
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2]) });
        var wallpaper = CreateWallpaper(id: "wallpaper-11", path: "https://example.test/image.jpg");

        _ = await downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None);

        (fileSystem.Directory.GetFiles(directory).Length, Convert.ToHexString(await fileSystem.File.ReadAllBytesAsync(finalPath, TestContext.Current.CancellationToken))).ShouldBe((1, "0102"));
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2025:Ensure tasks using 'IDisposable' instances complete before the instances are disposed", Justification = "The download is awaited before the client goes out of scope; the clock must advance while it is pending.")]
    public async Task when_pacing_is_configured_then_the_download_waits_for_the_delay_before_requesting_the_image()
    {
        var clock = new FakeTimeProvider();
        var pacedDownloader = new ImageDownloader(fileSystem, clock, new DownloadPacing(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)), ScrapeTimeouts.Default);
        var requestCount = 0;
        using var client = CreateClient(_ =>
        {
            requestCount++;

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2]) };
        });
        var wallpaper = CreateWallpaper(id: "paced-1", path: "https://example.test/image.jpg");
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");

        var download = pacedDownloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(1.9));
        var requestsBeforeTheDelay = requestCount;
        clock.Advance(TimeSpan.FromSeconds(1.1));
        _ = await download;

        (requestsBeforeTheDelay, requestCount).ShouldBe((0, 1));
    }

    private static async Task WaitForMessageCountAsync(CapturingProgress progress, int count)
    {
        for (var attempt = 0; attempt < 5000 && progress.Messages.Count < count; attempt++)
        {
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }

        progress.Messages.Count.ShouldBeGreaterThanOrEqualTo(count);
    }

    private static FileName NameFor(Data wallpaper, string extension) => new($"{wallpaper.Id}{extension}");

    private static Data CreateWallpaper(string id, string path = "", int fileSize = 0)
        => new(id, 0, 0, fileSize, "", path);

    [Fact]
    public async Task when_downloading_an_image_times_out_without_a_cancellation_then_a_timeout_is_thrown_and_writes_no_file()
    {
        using var client = CreateClient(_ => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout."));
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var wallpaper = CreateWallpaper(id: "wallpaper-timeout", path: "https://example.test/image.jpg");

        await Should.ThrowAsync<TimeoutException>(
            () => downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None));

        fileSystem.File.Exists(fileSystem.Path.Combine(directory, "wallpaper-timeout.jpg")).ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_scrape_is_cancelled_while_downloading_then_the_cancellation_is_not_turned_into_a_timeout()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        using var client = CreateClient(_ =>
        {
            cancellationTokenSource.Cancel();

            throw new TaskCanceledException("cancelled", null, cancellationTokenSource.Token);
        });
        var wallpaper = CreateWallpaper(id: "wallpaper-cancelled", path: "https://example.test/image.jpg");

        var thrown = await Should.ThrowAsync<OperationCanceledException>(
            () => downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, "root-directory", NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, cancellationTokenSource.Token));

        thrown.ShouldNotBeOfType<TimeoutException>();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2025:Ensure tasks using 'IDisposable' instances complete before the instances are disposed", Justification = "The download is awaited before the client goes out of scope; the clock must advance while it is pending.")]
    public async Task when_the_image_body_stalls_past_the_body_timeout_then_a_timeout_is_thrown_and_no_file_is_left()
    {
        var clock = new FakeTimeProvider();
        var timeouts = new ScrapeTimeouts(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
        var stalledDownloader = new ImageDownloader(fileSystem, clock, DownloadPacing.None, timeouts);
        var stalledStream = new StalledStream();
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stalledStream) });
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var wallpaper = CreateWallpaper(id: "stalled-1", path: "https://example.test/image.jpg");

        var download = stalledDownloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None);
        await stalledStream.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        clock.Advance(timeouts.ImageBody);

        await Should.ThrowAsync<TimeoutException>(() => download);
        fileSystem.Directory.GetFiles(directory).ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2025:Ensure tasks using 'IDisposable' instances complete before the instances are disposed", Justification = "The download is awaited before the client goes out of scope; the scrape is cancelled while it is pending.")]
    public async Task when_the_scrape_is_cancelled_while_the_image_body_is_stalled_then_the_cancellation_is_not_turned_into_a_timeout()
    {
        var stalledStream = new StalledStream();
        using var cancellationTokenSource = new CancellationTokenSource();
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stalledStream) });
        var wallpaper = CreateWallpaper(id: "stalled-2", path: "https://example.test/image.jpg");

        var download = downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, "root-directory", NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, cancellationTokenSource.Token);
        await stalledStream.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await cancellationTokenSource.CancelAsync();

        var thrown = await Should.ThrowAsync<OperationCanceledException>(() => download);
        thrown.ShouldNotBeOfType<TimeoutException>();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2025:Ensure tasks using 'IDisposable' instances complete before the instances are disposed", Justification = "The download is awaited before the client goes out of scope; the clock must advance while it is pending.")]
    public async Task when_the_image_body_is_still_downloading_then_the_bytes_received_so_far_are_reported_every_period()
    {
        var clock = new FakeTimeProvider();
        var slowDownloader = new ImageDownloader(fileSystem, clock, DownloadPacing.None, ScrapeTimeouts.Default);
        var slowStream = new SlowStream(new byte[2048]);
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(slowStream) });
        var progress = new CapturingProgress();
        var wallpaper = CreateWallpaper(id: "slow-1", path: "https://example.test/image.jpg", fileSize: 4096);

        var download = slowDownloader.DownloadAsync(new WallpaperFileRequest(wallpaper, "root-directory", NameFor(wallpaper, ".jpg"), "Top Wallpapers"), progress, client, TestContext.Current.CancellationToken);
        await slowStream.WaitingForMoreData.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        clock.Advance(ImageDownloader.ProgressEvery);
        await WaitForMessageCountAsync(progress, 2);
        clock.Advance(ImageDownloader.ProgressEvery);
        await WaitForMessageCountAsync(progress, 3);
        slowStream.Finish();
        _ = await download;

        progress.Messages.Skip(1).ShouldBe(
        [
            "Still downloading wallpaper slow-1 - 2 KB of 4 KB received so far.",
            "Still downloading wallpaper slow-1 - 2 KB of 4 KB received so far."
        ]);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2025:Ensure tasks using 'IDisposable' instances complete before the instances are disposed", Justification = "The download is awaited before the client goes out of scope; the clock must advance while it is pending.")]
    public async Task when_the_total_size_is_not_known_then_the_progress_message_omits_it()
    {
        var clock = new FakeTimeProvider();
        var slowDownloader = new ImageDownloader(fileSystem, clock, DownloadPacing.None, ScrapeTimeouts.Default);
        var slowStream = new SlowStream(new byte[2048]);
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(slowStream) });
        var progress = new CapturingProgress();
        var wallpaper = CreateWallpaper(id: "slow-2", path: "https://example.test/image.jpg", fileSize: 0);

        var download = slowDownloader.DownloadAsync(new WallpaperFileRequest(wallpaper, "root-directory", NameFor(wallpaper, ".jpg"), "Top Wallpapers"), progress, client, TestContext.Current.CancellationToken);
        await slowStream.WaitingForMoreData.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        clock.Advance(ImageDownloader.ProgressEvery);
        await WaitForMessageCountAsync(progress, 2);
        slowStream.Finish();
        _ = await download;

        progress.Messages.Skip(1).ShouldBe(["Still downloading wallpaper slow-2 - 2 KB received so far."]);
    }

    [Fact]
    public async Task when_the_image_body_arrives_promptly_then_no_progress_message_is_reported()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });
        var progress = new CapturingProgress();
        var wallpaper = CreateWallpaper(id: "fast-1", path: "https://example.test/image.jpg");

        _ = await downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, "root-directory", NameFor(wallpaper, ".jpg"), "Top Wallpapers"), progress, client, CancellationToken.None);

        progress.Messages.Count.ShouldBe(1);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient owns and disposes the handler.")]
    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => new(new StubHttpMessageHandler(responder));

    private sealed class StalledStream : Stream
    {
        public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _ = ReadStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class SlowStream(byte[] firstChunk) : Stream
    {
        private readonly TaskCompletionSource finish = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool firstChunkSent;

        public TaskCompletionSource WaitingForMoreData { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public void Finish() => finish.SetResult();

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!firstChunkSent)
            {
                firstChunkSent = true;
                firstChunk.CopyTo(buffer);

                return firstChunk.Length;
            }

            _ = WaitingForMoreData.TrySetResult();
            await finish.Task.WaitAsync(cancellationToken);

            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class FailingStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("connection reset");

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new IOException("connection reset");

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new IOException("connection reset");

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
