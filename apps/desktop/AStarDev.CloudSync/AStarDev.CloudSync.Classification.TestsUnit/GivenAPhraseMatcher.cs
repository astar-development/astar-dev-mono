using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAPhraseMatcher
{
    [Fact]
    public void when_a_longer_phrase_matches_then_it_wins_over_its_words()
    {
        var terms = new[] { Term("red", Colour), Term("car", Place), Term("red car", Place) };

        var matches = PhraseMatcher.Match(["red", "car"], terms);

        matches.Select(match => match.Text).ShouldBe(["red car"]);
    }

    [Fact]
    public void when_the_long_phrase_does_not_match_then_single_words_are_found()
    {
        var terms = new[] { Term("red", Colour), Term("car", Place), Term("red car", Place) };

        var matches = PhraseMatcher.Match(["red", "bike", "car"], terms);

        matches.Select(match => match.Text).ShouldBe(["red", "car"]);
    }

    [Fact]
    public void when_the_term_is_plural_then_the_singular_token_matches()
    {
        var matches = PhraseMatcher.Match(["car"], [Term("Cars", Place)]);

        matches.Count.ShouldBe(1);
    }

    [Fact]
    public void when_a_text_is_used_in_two_categories_then_both_terms_match()
    {
        var matches = PhraseMatcher.Match(["china"], [Term("china", Colour), Term("china", Place)]);

        matches.Count.ShouldBe(2);
    }

    [Fact]
    public void when_there_are_no_terms_or_tokens_then_nothing_matches()
    {
        PhraseMatcher.Match(["red"], []).ShouldBeEmpty();
        PhraseMatcher.Match([], [Term("red", Colour)]).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_term_has_no_word_characters_then_it_never_matches()
    {
        PhraseMatcher.Match(["red"], [Term("123", Colour)]).ShouldBeEmpty();
    }
}
