using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenUnsavedEdits
{
    [Fact]
    public void when_the_inputs_read_back_equal_the_baseline_then_there_are_no_unsaved_edits()
    {
        var baseline = ConfigurationEditInputs.From(GivenAConfigurationEditInputs.CreateEntity());

        var hasEdits = UnsavedEdits.Exist(baseline, () => baseline with { SearchCategories = new([.. baseline.SearchCategories.Categories]), PersonCategories = new([.. baseline.PersonCategories.Categories]) });

        hasEdits.ShouldBeFalse();
    }

    [Fact]
    public void when_a_scalar_section_differs_then_there_are_unsaved_edits()
    {
        var baseline = ConfigurationEditInputs.From(GivenAConfigurationEditInputs.CreateEntity());

        var hasEdits = UnsavedEdits.Exist(baseline, () => baseline with { User = baseline.User with { Username = "changed" } });

        hasEdits.ShouldBeTrue();
    }

    [Fact]
    public void when_a_category_row_is_added_then_there_are_unsaved_edits()
    {
        var baseline = ConfigurationEditInputs.From(GivenAConfigurationEditInputs.CreateEntity());

        var hasEdits = UnsavedEdits.Exist(baseline, () => baseline with { PersonCategories = new([.. baseline.PersonCategories.Categories, new PersonCategoryInput(Option.None<Guid>(), "new")]) });

        hasEdits.ShouldBeTrue();
    }

    [Fact]
    public void when_the_editor_inputs_cannot_be_read_then_there_are_unsaved_edits()
    {
        var baseline = ConfigurationEditInputs.From(GivenAConfigurationEditInputs.CreateEntity());

        var hasEdits = UnsavedEdits.Exist(baseline, () => throw new OverflowException("not a number"));

        hasEdits.ShouldBeTrue();
    }
}
