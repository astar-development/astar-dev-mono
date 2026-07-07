namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit;

public sealed class ScrapperConstantsShould
{
    [Fact]
    public void HaveTheExpectedImagesPerPage() => ScrapperConstants.ImagesPerPage.ShouldBe(24);

    [Fact]
    public void HaveTheExpectedThumbnailSize() => ScrapperConstants.ThumbnailSize.ShouldBe(500);

    [Fact]
    public void HaveTheExpectedThumbnailCornerRadius() => ScrapperConstants.ThumbnailCornerRadius.ShouldBe(20f);

    [Fact]
    public void HaveTheExpectedRetryDelay() => ScrapperConstants.RetryDelay.ShouldBe(TimeSpan.FromSeconds(10));
}
