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

    [Theory]
    [InlineData("Movies", "movie")]
    [InlineData("movie", "movies")]
    [InlineData("caches", "cache")]
    [InlineData("cache", "caches")]
    [InlineData("buses", "bus")]
    [InlineData("houses", "house")]
    [InlineData("cookies", "cookie")]
    [InlineData("news", "news")]
    [InlineData("lenses", "lens")]
    public void when_a_word_and_its_plural_meet_then_they_match(string folderWord, string termText)
    {
        var matches = PhraseMatcher.Match(ClassificationTokeniser.Tokenise(folderWord), [Term(termText, Place)]);

        matches.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("news", "new")]
    [InlineData("lens", "len")]
    public void when_two_unrelated_words_differ_only_by_a_trailing_s_then_they_do_not_match(string folderWord, string termText)
    {
        var matches = PhraseMatcher.Match(ClassificationTokeniser.Tokenise(folderWord), [Term(termText, Place)]);

        matches.ShouldBeEmpty();
    }

    [Fact]
    public void when_phrases_overlap_then_the_leftmost_longest_wins_and_tokens_are_not_reused()
    {
        var terms = new[] { Term("red car", Place), Term("car park", Place) };

        var matches = PhraseMatcher.Match(["red", "car", "park"], terms);

        matches.Select(match => match.Text).ShouldBe(["red car"]);
    }
}
