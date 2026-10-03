using AStarDev.WallpaperScraper.WallpaperIngestion;

namespace AStarDev.WallpaperScraper.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperInfo
{
    [Fact]
    public void when_the_search_total_is_known_then_the_category_description_shows_the_current_and_total_counts()
        => new WallpaperInfo("name", "Top Wallpapers", 1, 1, 1, new SearchCount(3, 1124)).CategoryDescription.ShouldBe(string.Create(System.Globalization.CultureInfo.CurrentCulture, $"Top Wallpapers ({3:N0} of {1124:N0})"));

    [Fact]
    public void when_the_search_total_is_not_known_then_the_category_description_is_just_the_label()
        => new WallpaperInfo("name", "Top Wallpapers", 1, 1, 1).CategoryDescription.ShouldBe("Top Wallpapers");
}
