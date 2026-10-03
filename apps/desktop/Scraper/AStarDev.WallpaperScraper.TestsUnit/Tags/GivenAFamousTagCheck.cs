using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.DetailResponse;
using AStarDev.WallpaperScraper.Tags;

namespace AStarDev.WallpaperScraper.TestsUnit.Tags;

public sealed class GivenAFamousTagCheck
{
    private static readonly string[] PersonCategories = ["Celebrities", "Models", "Pornstars", "Other Figures", "Actress"];

    [Theory]
    [InlineData("Celebrities")]
    [InlineData("Models")]
    [InlineData("Pornstars")]
    [InlineData("Other Figures")]
    [InlineData("Actress")]
    [InlineData("other figures")]
    public void when_a_capitalised_tag_is_in_a_person_category_then_it_is_famous(string category)
        => FamousTagCheck.IsFamous(Tag("Max Verstappen", category), PersonCategories).ShouldBeTrue();

    [Fact]
    public void when_a_descriptive_tag_is_in_a_person_category_then_it_is_not_famous()
        => FamousTagCheck.IsFamous(Tag("finger pointing", "Other Figures"), PersonCategories).ShouldBeFalse();

    [Fact]
    public void when_a_capitalised_tag_is_outside_the_person_categories_then_it_is_not_famous()
        => FamousTagCheck.IsFamous(Tag("Ansel Adams", "Photographers"), PersonCategories).ShouldBeFalse();

    [Fact]
    public void when_a_capitalised_tag_has_no_category_then_it_is_not_famous()
        => FamousTagCheck.IsFamous(Tag("Max Verstappen", ""), PersonCategories).ShouldBeFalse();

    [Fact]
    public void when_no_person_categories_are_configured_then_nothing_is_famous()
        => FamousTagCheck.IsFamous(Tag("Emma Watson", "Celebrities"), []).ShouldBeFalse();

    [Fact]
    public void when_a_tag_has_no_name_then_it_is_not_famous()
        => FamousTagCheck.IsFamous(Tag("", "Celebrities"), PersonCategories).ShouldBeFalse();

    private static Tag Tag(string name, string category) => new(1, name, name, 1, category, "sfw");
}
