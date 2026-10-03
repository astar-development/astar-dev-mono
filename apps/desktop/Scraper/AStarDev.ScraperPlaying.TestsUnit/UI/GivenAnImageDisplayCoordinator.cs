using AStarDev.ScraperPlaying.UI;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenAnImageDisplayCoordinator
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private readonly IImageDownloadNotifier notifier = new ImageDownloadNotifier();
    private readonly FakeDecoder decoder = new();

    [Fact]
    public async Task when_a_notified_image_decodes_successfully_then_image_ready_is_raised_with_the_decoded_stream_and_details()
    {
        using var decodedStream = new MemoryStream([1, 2, 3]);
        decoder.Decode = _ => decodedStream;
        var coordinator = new ImageDisplayCoordinator(notifier, decoder);
        var received = ExpectImages(coordinator, 1);

        notifier.NotifyImageDownloaded(Details("wallpaper-1"));

        var preview = (await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken)).Single();
        (preview.PngStream, preview.Info).ShouldBe((decodedStream, new WallpaperInfo("wallpaper-1", "Cars", 1234, 1920, 1080)));
    }

    [Fact]
    public async Task when_a_notified_image_is_decoded_then_it_is_decoded_at_the_preview_size()
    {
        var coordinator = new ImageDisplayCoordinator(notifier, decoder);
        var received = ExpectImages(coordinator, 1);

        notifier.NotifyImageDownloaded(Details("wallpaper-1"));

        _ = await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        decoder.MaximumDimensions.Single().ShouldBe(ImageDisplayCoordinator.PreviewMaximumDimension);
    }

    [Fact]
    public async Task when_a_notified_image_is_decoded_then_the_notifying_thread_is_not_blocked_by_the_decode()
    {
        using var release = new ManualResetEventSlim();
        decoder.Decode = _ =>
        {
            release.Wait(Timeout);

            return Stream.Null;
        };
        _ = new ImageDisplayCoordinator(notifier, decoder);

        var notify = Task.Run(() => notifier.NotifyImageDownloaded(Details("wallpaper-1")), TestContext.Current.CancellationToken);
        try
        {
            await notify.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        }
        finally
        {
            release.Set();
        }

        notify.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public void when_display_is_disabled_then_a_notified_image_is_not_decoded()
    {
        var coordinator = new ImageDisplayCoordinator(notifier, decoder) { IsEnabled = false };
        var raised = false;
        coordinator.ImageReady += (_, _) => raised = true;

        notifier.NotifyImageDownloaded(Details("wallpaper-1"));

        (decoder.DecodedPaths.Count, raised).ShouldBe((0, false));
    }

    [Fact]
    public void when_display_is_disabled_then_a_notified_images_details_are_still_raised_without_decoding()
    {
        var coordinator = new ImageDisplayCoordinator(notifier, decoder) { IsEnabled = false };
        List<WallpaperInfo> received = [];
        coordinator.DetailsReady += (_, info) => received.Add(info);

        notifier.NotifyImageDownloaded(Details("wallpaper-1"));

        (received.Single().Name, decoder.DecodedPaths.Count).ShouldBe(("wallpaper-1", 0));
    }

    [Fact]
    public async Task when_display_is_enabled_then_details_are_not_raised_separately_from_the_image()
    {
        var coordinator = new ImageDisplayCoordinator(notifier, decoder);
        var raised = false;
        coordinator.DetailsReady += (_, _) => raised = true;
        var received = ExpectImages(coordinator, 1);

        notifier.NotifyImageDownloaded(Details("wallpaper-1"));

        _ = await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task when_display_is_enabled_again_then_notified_images_are_decoded_again()
    {
        var coordinator = new ImageDisplayCoordinator(notifier, decoder) { IsEnabled = false };
        coordinator.IsEnabled = true;
        var received = ExpectImages(coordinator, 1);

        notifier.NotifyImageDownloaded(Details("wallpaper-1"));

        (await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken)).Single().Info.Name.ShouldBe("wallpaper-1");
    }

    [Fact]
    public async Task when_images_arrive_while_a_decode_is_running_then_only_the_latest_of_them_is_decoded_next()
    {
        using var firstDecodeStarted = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        decoder.Decode = path =>
        {
            if (path.Contains("wallpaper-1", StringComparison.Ordinal))
            {
                firstDecodeStarted.Set();
                release.Wait(Timeout);
            }

            return Stream.Null;
        };
        var coordinator = new ImageDisplayCoordinator(notifier, decoder);
        var received = ExpectImages(coordinator, 2);
        notifier.NotifyImageDownloaded(Details("wallpaper-1"));
        firstDecodeStarted.Wait(Timeout, TestContext.Current.CancellationToken).ShouldBeTrue();

        notifier.NotifyImageDownloaded(Details("wallpaper-2"));
        notifier.NotifyImageDownloaded(Details("wallpaper-3"));
        release.Set();

        var names = (await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken)).Select(preview => preview.Info.Name);
        (string.Join(",", names), decoder.DecodedPaths.Count).ShouldBe(("wallpaper-1,wallpaper-3", 2));
    }

    [Fact]
    public async Task when_a_notified_image_fails_to_decode_then_image_ready_is_not_raised_and_later_images_are_still_decoded()
    {
        decoder.Decode = path => path.Contains("bad", StringComparison.Ordinal) ? throw new InvalidOperationException("bad image") : Stream.Null;
        var coordinator = new ImageDisplayCoordinator(notifier, decoder);
        var received = ExpectImages(coordinator, 1);

        notifier.NotifyImageDownloaded(Details("bad"));
        await WaitForDecodesAsync(1);
        notifier.NotifyImageDownloaded(Details("good"));

        (await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken)).Single().Info.Name.ShouldBe("good");
    }

    private static WallpaperDownloadDetails Details(string name) => new($"/some/path/{name}.jpg", new WallpaperInfo(name, "Cars", 1234, 1920, 1080));

    private static TaskCompletionSource<IReadOnlyList<WallpaperPreviewImage>> ExpectImages(ImageDisplayCoordinator coordinator, int count)
    {
        var completion = new TaskCompletionSource<IReadOnlyList<WallpaperPreviewImage>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var images = new List<WallpaperPreviewImage>();
        coordinator.ImageReady += (_, preview) =>
        {
            images.Add(preview);
            if (images.Count == count) completion.TrySetResult(images);
        };

        return completion;
    }

    private async Task WaitForDecodesAsync(int count)
    {
        for (var attempt = 0; attempt < 1000 && decoder.DecodedPaths.Count < count; attempt++)
        {
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }
    }

    private sealed class FakeDecoder : IDownloadedImageDecoder
    {
        private readonly Lock gate = new();
        private readonly List<string> decodedPaths = [];
        private readonly List<int> maximumDimensions = [];

        public Func<string, Stream> Decode { get; set; } = _ => Stream.Null;

        public IReadOnlyList<string> DecodedPaths
        {
            get
            {
                lock (gate) return [.. decodedPaths];
            }
        }

        public IReadOnlyList<int> MaximumDimensions
        {
            get
            {
                lock (gate) return [.. maximumDimensions];
            }
        }

        public Stream DecodeToPng(string filePath, int maxDimension)
        {
            lock (gate)
            {
                decodedPaths.Add(filePath);
                maximumDimensions.Add(maxDimension);
            }

            return Decode(filePath);
        }
    }
}
