using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.DetailResponse;
using AStarDev.WallpaperScraper.WallpaperIngestion;

namespace AStarDev.WallpaperScraper.TestsUnit.WallpaperIngestion;

public sealed class GivenASaveDirectories
{
    private static readonly SaveDirectories Directories = new("normal", "famous", "outside");

    [Fact]
    public void when_the_wallpaper_has_no_tags_then_the_normal_root_and_the_category_are_used()
        => Directories.For([]).ShouldBe(Path.Combine("normal", "outside"));

    [Fact]
    public void when_no_tag_is_famous_then_the_normal_root_is_used()
        => Directories.For([Tag("one"), Tag("two")]).ShouldBe(Path.Combine("normal", "outside"));

    [Fact]
    public void when_any_tag_is_famous_then_the_famous_root_is_used()
        => Directories.For([Tag("one"), Tag("two", famous: true)]).ShouldBe(Path.Combine("famous", "two", "outside"));

    [Fact]
    public void when_a_tag_is_a_name_then_its_slug_is_prepended_to_the_category()
        => Directories.For([Tag("Some Name", isName: true)]).ShouldBe(Path.Combine("normal", "some-name", "outside"));

    [Fact]
    public void when_several_tags_are_names_then_their_slugs_are_joined_with_an_underscore_in_tag_order()
        => Directories.For([Tag("Name B", isName: true), Tag("other"), Tag("Name A", isName: true)]).ShouldBe(Path.Combine("normal", "name-b_name-a", "outside"));

    [Fact]
    public void when_a_tag_is_a_famous_name_then_the_name_is_prepended_under_the_famous_root()
        => Directories.For([Tag("Some Name", famous: true, isName: true)]).ShouldBe(Path.Combine("famous", "some-name", "outside"));

    [Fact]
    public void when_a_tag_is_famous_but_not_flagged_as_a_name_then_its_name_is_still_prepended()
        => Directories.For([Tag("Emma Stone", famous: true)]).ShouldBe(Path.Combine("famous", "emma-stone", "outside"));

    [Fact]
    public void when_a_famous_tag_and_a_name_tag_are_present_then_both_are_prepended_in_tag_order()
        => Directories.For([Tag("Emma Stone", famous: true), Tag("Other Name", isName: true)]).ShouldBe(Path.Combine("famous", "emma-stone_other-name", "outside"));

    [Fact]
    public void when_a_tag_is_both_famous_and_a_name_then_its_name_is_prepended_once()
        => Directories.For([Tag("Emma Stone", famous: true, isName: true)]).ShouldBe(Path.Combine("famous", "emma-stone", "outside"));

    [Fact]
    public void when_a_name_has_invalid_path_characters_then_they_are_removed()
        => Directories.For([Tag("AC/DC: \"Live\"?", isName: true)]).ShouldBe(Path.Combine("normal", "acdc-live", "outside"));

    [Theory]
    [InlineData("???")]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("   ")]
    public void when_a_name_leaves_nothing_usable_then_it_is_skipped(string name)
        => Directories.For([Tag(name, isName: true)]).ShouldBe(Path.Combine("normal", "outside"));

    [Fact]
    public void when_a_name_starts_or_ends_with_dots_then_they_are_trimmed()
        => Directories.For([Tag("..sneaky..", isName: true)]).ShouldBe(Path.Combine("normal", "sneaky", "outside"));

    [Fact]
    public void when_the_names_are_very_long_then_they_are_truncated()
        => Directories.For([Tag(new string('a', 400), isName: true)]).ShouldBe(Path.Combine("normal", new string('a', 100), "outside"));

    private static Tag Tag(string name, bool famous = false, bool isName = false) => new(1, name, name, 1, "Category", "sfw", IsName: isName, IsFamous: famous);
}
