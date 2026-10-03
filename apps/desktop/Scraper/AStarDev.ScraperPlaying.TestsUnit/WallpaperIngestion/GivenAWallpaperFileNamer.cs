using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperFileNamer
{
    [Fact]
    public void when_the_wallpaper_has_no_famous_tag_then_the_wallpaper_id_is_the_file_name()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Tag("anime"), Tag("Emma Watson"), Tag("men")]).Value.ShouldBe("abc123.jpg");

    [Fact]
    public void when_the_wallpaper_has_no_tags_then_the_wallpaper_id_is_the_file_name()
        => WallpaperFileNamer.Create("abc123", ".png", []).Value.ShouldBe("abc123.png");

    [Fact]
    public void when_the_wallpaper_has_a_famous_tag_then_its_name_prefixes_the_file_name()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Famous("Max Verstappen"), Tag("men")]).Value.ShouldBe("Max_Verstappen_abc123.jpg");

    [Fact]
    public void when_a_famous_tag_is_not_capitalised_then_it_is_still_used()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Famous("max verstappen")]).Value.ShouldBe("max_verstappen_abc123.jpg");

    [Fact]
    public void when_the_wallpaper_has_several_famous_tags_then_they_are_all_joined_in_page_order()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Famous("Emma Watson"), Tag("brunette"), Famous("Daniel Radcliffe")]).Value.ShouldBe("Emma_Watson_Daniel_Radcliffe_abc123.jpg");

    [Fact]
    public void when_a_famous_name_has_invalid_file_name_characters_then_they_are_removed()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Famous("AC/DC: \"Live\"?")]).Value.ShouldBe("ACDC_Live_abc123.jpg");

    [Fact]
    public void when_a_famous_name_is_only_invalid_file_name_characters_then_it_is_skipped()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Famous("???")]).Value.ShouldBe("abc123.jpg");

    [Fact]
    public void when_the_prefix_is_very_long_then_it_is_truncated_so_the_file_name_stays_within_limits()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Famous($"A{new string('a', 400)}")]).Value.ShouldBe($"A{new string('a', 99)}_abc123.jpg");

    private static Tag Tag(string name) => new(1, name, name, 1, "Celebrities", "sfw");

    private static Tag Famous(string name) => Tag(name) with { IsFamous = true };
}
