using AStar.Dev.Wallpaper.Scraper.Scraping.Thumbnails;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping.Thumbnails;

public sealed class GivenWallpaperThumbnailBroadcaster : IDisposable
{
    private readonly WallpaperThumbnailBroadcaster sut = new();
    private bool disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (disposing)
            sut.Dispose();
    }

    [Fact]
    public void when_a_thumbnail_is_published_then_subscribers_to_the_feed_receive_it()
    {
        var payload = WallpaperThumbnailPayloadFactory.Create([1, 2, 3], "Nature", ["forest"]);
        WallpaperThumbnailPayload? received = null;
        using var subscription = sut.Thumbnails.Subscribe(publishedPayload => received = publishedPayload);

        sut.Publish(payload);

        received.ShouldBe(payload);
    }

    [Fact]
    public void when_no_subscriber_is_present_then_publishing_does_not_throw()
    {
        var payload = WallpaperThumbnailPayloadFactory.Create([1, 2, 3], "Nature", ["forest"]);

        var exception = Record.Exception(() => sut.Publish(payload));

        exception.ShouldBeNull();
    }

    [Fact]
    public void when_a_category_skip_is_published_then_subscribers_to_the_feed_receive_the_category_name()
    {
        string? received = null;
        using var subscription = sut.CategorySkipped.Subscribe(categoryName => received = categoryName);

        sut.PublishCategorySkipped("Nature");

        received.ShouldBe("Nature");
    }

    [Fact]
    public void when_no_subscriber_is_present_then_publishing_a_category_skip_does_not_throw()
    {
        var exception = Record.Exception(() => sut.PublishCategorySkipped("Nature"));

        exception.ShouldBeNull();
    }
}
