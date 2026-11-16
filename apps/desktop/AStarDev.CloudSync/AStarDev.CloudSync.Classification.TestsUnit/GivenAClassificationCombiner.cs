using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAClassificationCombiner
{
    private static readonly ClassificationCategory ColourCategory = ClassificationCategoryFactory.Create(Colour, "Colour");

    [Fact]
    public void when_the_same_term_is_low_then_high_then_the_high_candidate_is_kept()
    {
        var low = Candidate(Term("china", Colour), ClassificationConfidenceFactory.CreateLow());
        var high = Candidate(Term("china", Colour), ClassificationConfidenceFactory.CreateHigh());

        var result = ClassificationCombiner.Combine(PathOf("a/b.jpg"), [low, high]);

        result.Candidates.ShouldBe([high]);
    }

    [Fact]
    public void when_the_same_text_and_category_come_from_different_term_ids_then_one_is_kept()
    {
        var first = Candidate(Term("red", Colour), ClassificationConfidenceFactory.CreateHigh());
        var second = Candidate(Term("RED", Colour), ClassificationConfidenceFactory.CreateHigh());

        var result = ClassificationCombiner.Combine(PathOf("a/b.jpg"), [first, second]);

        result.Candidates.Count.ShouldBe(1);
    }

    [Fact]
    public void when_the_same_text_is_in_two_categories_then_both_are_kept()
    {
        var colour = Candidate(Term("china", Colour), ClassificationConfidenceFactory.CreateHigh());
        var place = Candidate(Term("china", Place), ClassificationConfidenceFactory.CreateHigh());

        var result = ClassificationCombiner.Combine(PathOf("a/b.jpg"), [colour, place]);

        result.Candidates.Count.ShouldBe(2);
    }

    [Fact]
    public void when_a_term_is_ignored_then_its_candidate_is_dropped()
    {
        var ignored = Candidate(Term("red", Colour, TermSourceFactory.CreateSeed(), TermStatusFactory.CreateIgnored()), ClassificationConfidenceFactory.CreateHigh());

        var result = ClassificationCombiner.Combine(PathOf("a/b.jpg"), [ignored]);

        result.Candidates.ShouldBeEmpty();
    }

    [Fact]
    public void when_there_are_no_candidates_then_the_file_is_unclassified()
    {
        var result = ClassificationCombiner.Combine(PathOf("a/b.jpg"), []);

        result.Candidates.ShouldBeEmpty();
    }

    private static ClassificationCandidate Candidate(ClassificationTerm term, ClassificationConfidence confidence) =>
        ClassificationCandidateFactory.Create(term, ColourCategory, SignalKindFactory.CreateVocabulary(), confidence);
}
