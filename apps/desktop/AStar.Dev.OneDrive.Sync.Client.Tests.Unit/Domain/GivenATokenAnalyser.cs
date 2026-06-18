using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenATokenAnalyser
{
    [Fact]
    public void when_stop_words_accessed_then_common_articles_are_present()
    {
        TokenAnalyser.StopWords.ShouldContain("a");
        TokenAnalyser.StopWords.ShouldContain("an");
        TokenAnalyser.StopWords.ShouldContain("the");
        TokenAnalyser.StopWords.ShouldContain("on");
        TokenAnalyser.StopWords.ShouldContain("in");
        TokenAnalyser.StopWords.ShouldContain("of");
        TokenAnalyser.StopWords.ShouldContain("with");
    }

    [Fact]
    public void when_stop_words_accessed_then_filler_words_are_present()
    {
        TokenAnalyser.StopWords.ShouldContain("it");
        TokenAnalyser.StopWords.ShouldContain("its");
        TokenAnalyser.StopWords.ShouldContain("and");
        TokenAnalyser.StopWords.ShouldContain("at");
    }

    [Fact]
    public void when_colour_words_accessed_then_common_colours_are_present()
    {
        TokenAnalyser.ColourWords.ShouldContain("red");
        TokenAnalyser.ColourWords.ShouldContain("blue");
        TokenAnalyser.ColourWords.ShouldContain("green");
        TokenAnalyser.ColourWords.ShouldContain("black");
        TokenAnalyser.ColourWords.ShouldContain("white");
        TokenAnalyser.ColourWords.ShouldContain("yellow");
        TokenAnalyser.ColourWords.ShouldContain("pink");
        TokenAnalyser.ColourWords.ShouldContain("purple");
        TokenAnalyser.ColourWords.ShouldContain("orange");
        TokenAnalyser.ColourWords.ShouldContain("brown");
        TokenAnalyser.ColourWords.ShouldContain("grey");
        TokenAnalyser.ColourWords.ShouldContain("gray");
    }

    [Fact]
    public void when_extract_person_name_called_with_john_smith_text_then_name_is_returned()
    {
        var result = TokenAnalyser.ExtractPersonName("a file with a persons name: john smith - in it.jpg");

        result.MapOrDefault(v => v, string.Empty).ShouldBe("John Smith");
    }

    [Fact]
    public void when_extract_person_name_called_with_jane_doe_text_then_name_is_returned()
    {
        var result = TokenAnalyser.ExtractPersonName("jane doe birthday party.jpg");

        result.MapOrDefault(v => v, string.Empty).ShouldBe("Jane Doe");
    }

    [Fact]
    public void when_extract_person_name_called_with_no_person_name_then_none_is_returned()
    {
        var result = TokenAnalyser.ExtractPersonName("a red car on the road.jpg");

        result.MapOrDefault(v => false, true).ShouldBeTrue();
    }

    [Fact]
    public void when_extract_person_name_called_with_single_word_then_none_is_returned()
    {
        var result = TokenAnalyser.ExtractPersonName("red.jpg");

        result.MapOrDefault(v => false, true).ShouldBeTrue();
    }

    [Fact]
    public void when_extract_person_name_called_with_empty_string_then_none_is_returned()
    {
        var result = TokenAnalyser.ExtractPersonName(string.Empty);

        result.MapOrDefault(v => false, true).ShouldBeTrue();
    }

    [Fact]
    public void when_extract_colour_phrase_called_with_red_car_tokens_then_compound_phrase_is_returned()
    {
        var result = TokenAnalyser.ExtractColourPhrase(["red", "car", "road"]);

        result.MapOrDefault(v => v, string.Empty).ShouldBe("red car");
    }

    [Fact]
    public void when_extract_colour_phrase_called_with_red_dress_tokens_then_compound_phrase_is_returned()
    {
        var result = TokenAnalyser.ExtractColourPhrase(["red", "dress", "floor"]);

        result.MapOrDefault(v => v, string.Empty).ShouldBe("red dress");
    }

    [Fact]
    public void when_extract_colour_phrase_called_with_colour_followed_by_non_noun_then_colour_only_is_returned()
    {
        var result = TokenAnalyser.ExtractColourPhrase(["file", "red", "name"]);

        result.MapOrDefault(v => v, string.Empty).ShouldBe("red");
    }

    [Fact]
    public void when_extract_colour_phrase_called_with_no_colour_tokens_then_none_is_returned()
    {
        var result = TokenAnalyser.ExtractColourPhrase(["file", "something", "else"]);

        result.MapOrDefault(v => false, true).ShouldBeTrue();
    }

    [Fact]
    public void when_extract_colour_phrase_called_with_empty_list_then_none_is_returned()
    {
        var result = TokenAnalyser.ExtractColourPhrase([]);

        result.MapOrDefault(v => false, true).ShouldBeTrue();
    }

    [Fact]
    public void when_extract_colour_phrase_called_with_colour_only_token_then_colour_is_returned()
    {
        var result = TokenAnalyser.ExtractColourPhrase(["red"]);

        result.MapOrDefault(v => v, string.Empty).ShouldBe("red");
    }

    [Fact]
    public void when_plan_example_red_car_tokens_processed_then_correct_phrase_returned()
    {
        var result = TokenAnalyser.ExtractColourPhrase(["red", "car", "road"]);

        result.MapOrDefault(v => v, string.Empty).ShouldBe("red car");
    }

    [Fact]
    public void when_plan_example_red_dress_tokens_processed_then_correct_phrase_returned()
    {
        var result = TokenAnalyser.ExtractColourPhrase(["red", "dress", "floor"]);

        result.MapOrDefault(v => v, string.Empty).ShouldBe("red dress");
    }

    [Fact]
    public void when_plan_example_file_with_red_tokens_processed_then_colour_only_returned()
    {
        var result = TokenAnalyser.ExtractColourPhrase(["file", "red", "name"]);

        result.MapOrDefault(v => v, string.Empty).ShouldBe("red");
    }

    [Fact]
    public void when_plan_example_john_smith_text_processed_then_person_name_returned()
    {
        var result = TokenAnalyser.ExtractPersonName("a file with a persons name: john smith - in it.jpg");

        result.MapOrDefault(v => v, string.Empty).ShouldBe("John Smith");
    }
}
