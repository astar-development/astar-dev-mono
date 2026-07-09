namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit;

public sealed class ScraperConstantsShould
{
    [Fact]
    public void HaveTheExpectedImagesPerPage() => ScraperConstants.ImagesPerPage.ShouldBe(24);

    [Fact]
    public void HaveTheExpectedThumbnailSize() => ScraperConstants.ThumbnailSize.ShouldBe(500);

    [Fact]
    public void HaveTheExpectedThumbnailCornerRadius() => ScraperConstants.ThumbnailCornerRadius.ShouldBe(20f);

    [Fact]
    public void HaveTheExpectedRetryDelay() => ScraperConstants.RetryDelay.ShouldBe(TimeSpan.FromSeconds(10));
}
