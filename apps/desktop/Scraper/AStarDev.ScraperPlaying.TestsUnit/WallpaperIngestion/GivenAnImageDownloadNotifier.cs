using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAnImageDownloadNotifier
{
    private static readonly WallpaperDownloadDetails details = new("/some/path/wallpaper-1.jpg", new WallpaperInfo("wallpaper-1", "Top Wallpapers", 1234, 1920, 1080));

    [Fact]
    public void when_an_image_download_is_notified_then_subscribers_receive_the_details()
    {
        var notifier = new ImageDownloadNotifier();
        WallpaperDownloadDetails? received = null;
        notifier.ImageDownloaded += (_, value) => received = value;

        notifier.NotifyImageDownloaded(details);

        received.ShouldBe(details);
    }

    [Fact]
    public void when_notified_with_no_subscribers_then_it_does_not_throw()
    {
        var notifier = new ImageDownloadNotifier();

        Should.NotThrow(() => notifier.NotifyImageDownloaded(details));
    }
}
