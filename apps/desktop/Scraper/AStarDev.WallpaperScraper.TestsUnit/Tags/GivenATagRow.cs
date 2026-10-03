using AStarDev.WallpaperScraper.Tags;
using AStarDev.WallpaperScraper.UI;

namespace AStarDev.WallpaperScraper.TestsUnit.Tags;

public sealed class GivenATagRow
{
    private static readonly TagSummary Summary = new(7, "cats", "Nature", "sfw", false, false, false);

    [Fact]
    public void when_the_flag_is_untouched_then_the_row_is_not_changed()
    {
        var row = new TagRow(Summary);

        row.IsChanged.ShouldBeFalse();
    }

    [Fact]
    public void when_the_flag_is_toggled_then_the_row_is_changed()
    {
        var row = new TagRow(Summary) { IgnoreImage = true };

        row.IsChanged.ShouldBeTrue();
    }

    [Fact]
    public void when_the_flag_is_toggled_back_then_the_row_is_not_changed()
    {
        var row = new TagRow(Summary) { IgnoreImage = true };
        row.IgnoreImage = false;

        row.IsChanged.ShouldBeFalse();
    }

    [Fact]
    public void when_the_name_flag_is_toggled_then_the_row_is_changed()
    {
        var row = new TagRow(Summary) { IsName = true };

        row.IsChanged.ShouldBeTrue();
    }

    [Fact]
    public void when_the_famous_flag_is_toggled_then_the_row_is_changed()
    {
        var row = new TagRow(Summary) { IsFamous = true };

        row.IsChanged.ShouldBeTrue();
    }

    [Fact]
    public void when_flags_are_changed_then_the_row_reports_both_flags()
    {
        var row = new TagRow(Summary) { IgnoreImage = true, IsName = true, IsFamous = true };

        row.Flags.ShouldBe(new TagFlags(true, true, true));
    }

    [Fact]
    public void when_the_row_is_created_then_it_shows_the_tag_details()
    {
        var row = new TagRow(Summary);

        (row.WallhavenTagId, row.Name, row.Category, row.Purity).ShouldBe((7, "cats", "Nature", "sfw"));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("CAT", true)]
    [InlineData("nat", true)]
    [InlineData("dog", false)]
    public void when_a_filter_is_applied_then_the_row_matches_its_name_or_category_ignoring_case(string filter, bool expected) =>
        new TagRow(Summary).Matches(filter).ShouldBe(expected);
}
