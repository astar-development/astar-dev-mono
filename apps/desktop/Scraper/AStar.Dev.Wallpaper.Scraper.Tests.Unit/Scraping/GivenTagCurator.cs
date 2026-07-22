using AStar.Dev.Wallpaper.Scraper.Scraping;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenTagCurator
{
    [Fact]
    public void when_multiple_tags_are_curated_then_kept_tags_preserve_their_original_relative_order()
    {
        List<TagData> tags = [new("Nature", null), new("Ignored", null), new("City", null)];

        var curation = TagCurator.Curate(tags, ["ignored"], []);

        curation.Kept.ShouldBe([tags[0], tags[2]]);
    }

    [Fact]
    public void when_a_tag_is_exactly_model_then_it_is_dropped_entirely_and_not_reported()
    {
        List<TagData> tags = [new("model", "people > model")];

        var curation = TagCurator.Curate(tags, [], []);

        curation.Kept.ShouldBeEmpty();
        curation.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_tag_ends_with_model_then_the_suffix_is_stripped_before_it_is_kept()
    {
        List<TagData> tags = [new("Emma Stone Model", "people > model")];

        var curation = TagCurator.Curate(tags, [], []);

        curation.Kept.ShouldBe([new TagData("Emma Stone", "people > model")]);
        curation.Messages.ShouldContain(message => message.Contains("Found tag: Emma Stone,"));
    }

    [Fact]
    public void when_the_word_model_appears_mid_tag_then_only_the_trailing_suffix_is_stripped()
    {
        List<TagData> tags = [new("Super Model Model", "people > model")];

        var curation = TagCurator.Curate(tags, [], []);

        curation.Kept.ShouldBe([new TagData("Super Model", "people > model")]);
    }

    [Fact]
    public void when_a_tag_matches_the_ignore_list_then_only_the_found_message_is_recorded_and_the_kept_message_is_omitted()
    {
        List<TagData> tags = [new("Ignored", null)];

        var curation = TagCurator.Curate(tags, [], ["ignored"]);

        curation.Kept.ShouldBeEmpty();
        curation.Messages.ShouldBe(["Found tag: Ignored, category: , isFamous: False, isInternet: False"]);
    }

    [Fact]
    public void when_multiple_tags_are_curated_then_their_messages_are_folded_in_original_tag_order()
    {
        List<TagData> tags = [new("Nature", null), new("Ignored", null), new("City", null)];

        var curation = TagCurator.Curate(tags, ["ignored"], []);

        curation.Messages.ShouldBe(
        [
            "Found tag: Nature, category: , isFamous: False, isInternet: False",
            "Tag: Nature is not in the tagsToIgnore list, added to the list of tags to save to the database",
            "Found tag: Ignored, category: , isFamous: False, isInternet: False",
            "Found tag: City, category: , isFamous: False, isInternet: False",
            "Tag: City is not in the tagsToIgnore list, added to the list of tags to save to the database"
        ]);
    }

    [Fact]
    public void when_there_are_no_tags_then_an_empty_curation_is_returned()
    {
        var curation = TagCurator.Curate([], [], []);

        curation.Kept.ShouldBeEmpty();
        curation.Messages.ShouldBeEmpty();
    }
}
