using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAPhraseIndex
{
    public static TheoryData<string[], string[]> Cases => new()
    {
        { ["red", "car"], ["red", "car", "red car"] },
        { ["red", "bike", "car"], ["red", "car", "red car"] },
        { ["car"], ["Cars"] },
        { ["china"], ["china", "china"] },
        { ["red"], [] },
        { [], ["red"] },
        { ["red"], ["123"] },
        { ["movie"], ["Movies"] },
        { ["news"], ["new"] },
        { ["red", "car", "park"], ["red car", "car park"] },
        { ["red", "car", "park", "red", "car"], ["red car", "car park", "park"] }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void when_matched_through_the_index_then_the_result_equals_the_unindexed_match(string[] tokens, string[] termTexts)
    {
        var terms = termTexts.Select((text, position) => Term(text, position % 2 == 0 ? Colour : Place)).ToArray();

        var indexed = PhraseMatcher.Match(tokens, PhraseIndexFactory.Create(terms));

        indexed.ShouldBe(PhraseMatcher.Match(tokens, terms));
    }

    [Fact]
    public void when_one_index_serves_several_matches_then_each_match_is_correct()
    {
        var index = PhraseIndexFactory.Create([Term("red car", Place), Term("blue", Colour)]);

        var first = PhraseMatcher.Match(["red", "car"], index);
        var second = PhraseMatcher.Match(ClassificationTokeniser.Tokenise("blue"), index);
        var third = PhraseMatcher.Match(["green"], index);

        first.Select(match => match.Text).ShouldBe(["red car"]);
        second.Select(match => match.Text).ShouldBe(["blue"]);
        third.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_vocabulary_is_created_then_it_carries_one_index_for_its_terms()
    {
        var vocabulary = Vocabulary(Term("red car", Place));

        var matches = PhraseMatcher.Match(["red", "car"], vocabulary.PhraseIndex);

        (matches.Count, ReferenceEquals(vocabulary.PhraseIndex, vocabulary.PhraseIndex)).ShouldBe((1, true));
    }
}
