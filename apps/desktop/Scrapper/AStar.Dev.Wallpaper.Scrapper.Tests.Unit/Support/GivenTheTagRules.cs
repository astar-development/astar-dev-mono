using AStar.Dev.Wallpaper.Scrapper.DTOs;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Support;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenTheTagRules
{
    private const string InitialDirectory = "initial";
    private const string BaseDirectoryFamous = "famous";

    private static TagRuleContext Context(IEnumerable<string>? ignoreCompletely = null, IEnumerable<string>? textToIgnore = null)
        => TagRuleContextFactory.Create(
            InitialDirectory,
            BaseDirectoryFamous,
            new TagsToIgnoreCompletely { Tags = [.. ignoreCompletely ?? []], },
            new TagsTextToIgnore { Tags = [.. textToIgnore ?? []], });

    [Fact]
    public void when_there_are_no_tags_then_the_outcome_is_accepted_with_an_empty_prefix_and_only_the_initial_directory()
    {
        var outcome = TagRules.Evaluate([], Context()).ShouldBeOfType<Accept>();

        outcome.FilePrefix.ShouldBe(string.Empty);
        outcome.DirectorySegments.ShouldBe([InitialDirectory,]);
        outcome.Tags.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_tag_category_matches_the_ignore_completely_list_then_the_image_is_skipped()
    {
        List<TagData> tags = [new("Sports Car", "Vehicles > Cars & Motorcycles"), new("BadCategory", "ExcludedCategory"),];

        var outcome = TagRules.Evaluate(tags, Context(ignoreCompletely: ["ExcludedCategory",])).ShouldBeOfType<SkipImage>();

        outcome.Tags.ShouldBe(["Sports Car", "BadCategory",]);
    }

    [Fact]
    public void when_a_tag_text_matches_the_ignore_completely_list_then_the_image_is_skipped()
    {
        List<TagData> tags = [new("BadTagText", "SomeCategory"),];

        var outcome = TagRules.Evaluate(tags, Context(ignoreCompletely: ["BadTagText",])).ShouldBeOfType<SkipImage>();

        outcome.Tags.ShouldBe(["BadTagText",]);
    }

    [Fact]
    public void when_a_skip_match_occurs_mid_list_then_later_tags_are_not_processed_but_the_outcome_is_still_skipped()
    {
        List<TagData> tags = [new("Sports Car", "Vehicles > Cars & Motorcycles"), new("BadCategory", "ExcludedCategory"), new("LaterTag", "Vehicles > Cars & Motorcycles"),];

        var outcome = TagRules.Evaluate(tags, Context(ignoreCompletely: ["ExcludedCategory",])).ShouldBeOfType<SkipImage>();

        outcome.Tags.ShouldBe(["Sports Car", "BadCategory", "LaterTag",]);
    }

    [Fact]
    public void when_a_tag_has_a_null_category_then_it_is_excluded_from_rule_processing_but_its_text_is_kept_in_tags()
    {
        List<TagData> tags = [new("JustAText", null),];

        var outcome = TagRules.Evaluate(tags, Context()).ShouldBeOfType<Accept>();

        outcome.FilePrefix.ShouldBe(string.Empty);
        outcome.Tags.ShouldBe(["JustAText",]);
    }

    [Fact]
    public void when_a_people_model_category_tag_text_is_wanted_then_the_file_prefix_gains_the_tag_text()
    {
        List<TagData> tags = [new("Britney Spears", "People > Model"),];

        var outcome = TagRules.Evaluate(tags, Context()).ShouldBeOfType<Accept>();

        outcome.FilePrefix.ShouldBe("Britney Spears");
    }

    [Fact]
    public void when_a_people_model_category_tag_text_is_in_the_tags_text_to_ignore_list_then_the_file_prefix_is_not_updated()
    {
        List<TagData> tags = [new("Britney Spears", "People > Model"),];

        var outcome = TagRules.Evaluate(tags, Context(textToIgnore: ["Britney Spears",])).ShouldBeOfType<Accept>();

        outcome.FilePrefix.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_a_people_model_category_tag_text_starts_with_model_then_it_is_treated_as_unwanted_text_and_the_file_prefix_is_not_updated()
    {
        List<TagData> tags = [new("model Agency", "People > Model"),];

        var outcome = TagRules.Evaluate(tags, Context()).ShouldBeOfType<Accept>();

        outcome.FilePrefix.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_a_duplicate_people_model_tag_text_appears_twice_then_the_file_prefix_is_not_duplicated()
    {
        List<TagData> tags = [new("Famous Person", "People > Model"), new("Famous Person", "People > Model"),];

        var outcome = TagRules.Evaluate(tags, Context()).ShouldBeOfType<Accept>();

        outcome.FilePrefix.ShouldBe("Famous Person");
    }

    [Fact]
    public void when_a_tag_category_contains_vehicles_cars_and_motorcycles_then_the_file_prefix_is_never_updated()
    {
        List<TagData> tags = [new("AnyText", "Anything Vehicles > Cars & Motorcycles Anything"),];

        var outcome = TagRules.Evaluate(tags, Context()).ShouldBeOfType<Accept>();

        outcome.FilePrefix.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_a_tag_category_is_exactly_car_then_the_vehicles_branch_does_not_trigger_and_the_file_prefix_is_not_updated()
    {
        List<TagData> tags = [new("AnyText", "car"),];

        var outcome = TagRules.Evaluate(tags, Context()).ShouldBeOfType<Accept>();

        outcome.FilePrefix.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_a_celeb_related_category_tag_text_is_unwanted_and_already_contained_in_the_prefix_then_it_is_appended_lowercased_with_dashes_and_the_base_directory_famous_is_added()
    {
        List<TagData> tags = [new("Actress Extraordinaire", "People > Actress"), new("Actress", "Some > Actress Category"),];

        var outcome = TagRules.Evaluate(tags, Context(textToIgnore: ["Actress",])).ShouldBeOfType<Accept>();

        outcome.FilePrefix.ShouldBe("actress extraordinaire-actress");
        outcome.DirectorySegments.ShouldBe([InitialDirectory, BaseDirectoryFamous,]);
    }

    [Fact]
    public void when_a_singer_related_category_tag_text_is_unwanted_and_already_contained_in_the_prefix_then_it_is_appended_lowercased_with_dashes()
    {
        List<TagData> tags = [new("Singer Extraordinaire", "People > Singer"), new("Singer", "Some > Singer Category"),];

        var outcome = TagRules.Evaluate(tags, Context(textToIgnore: ["Singer",])).ShouldBeOfType<Accept>();

        outcome.FilePrefix.ShouldBe("singer extraordinaire-singer");
    }

    [Fact]
    public void when_the_final_file_prefix_would_start_with_a_dash_then_the_leading_dash_is_stripped()
    {
        List<TagData> tags = [new("Britney Spears", "People > Model"),];

        var outcome = TagRules.Evaluate(tags, Context()).ShouldBeOfType<Accept>();

        outcome.FilePrefix.ShouldStartWith("Britney");
        outcome.FilePrefix.ShouldNotStartWith("-");
    }

    [Fact]
    public void when_there_is_a_mix_of_wanted_null_and_empty_tags_then_only_non_whitespace_tag_text_is_kept_in_tags()
    {
        List<TagData> tags = [new("Nature", "Landscape"), new("Britney Spears", "People > Model"), new(string.Empty, "EmptyTagText"), new("Some Tag", null),];

        var outcome = TagRules.Evaluate(tags, Context()).ShouldBeOfType<Accept>();

        outcome.Tags.ShouldBe(["Nature", "Britney Spears", "Some Tag",]);
        outcome.FilePrefix.ShouldBe("Britney Spears");
    }
}
