using AStarDev.WallpaperScraper.WallpaperIngestion;

namespace AStarDev.WallpaperScraper.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperInfo
{
    [Fact]
    public void when_the_search_and_page_totals_are_known_then_the_category_description_is_indented_and_shows_the_image_and_page_counts()
        => new WallpaperInfo("name", "Cars", 1, 1, 1, new SearchCount(147, 2000, 6, 27)).CategoryDescription.ShouldBe(string.Create(System.Globalization.CultureInfo.CurrentCulture, $"Cars\t\t(Image {147:N0} of {2000:N0}, page {6:N0} of {27:N0})"));

    [Fact]
    public void when_only_the_search_total_is_known_then_the_category_description_omits_the_page_details()
        => new WallpaperInfo("name", "Top Wallpapers", 1, 1, 1, new SearchCount(3, 1124)).CategoryDescription.ShouldBe(string.Create(System.Globalization.CultureInfo.CurrentCulture, $"Top Wallpapers\t\t(Image {3:N0} of {1124:N0})"));

    [Fact]
    public void when_the_search_total_is_not_known_then_the_category_description_is_just_the_label()
        => new WallpaperInfo("name", "Top Wallpapers", 1, 1, 1).CategoryDescription.ShouldBe("Top Wallpapers");
}
