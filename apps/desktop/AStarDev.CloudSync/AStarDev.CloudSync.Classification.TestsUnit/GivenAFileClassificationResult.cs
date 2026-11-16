using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAFileClassificationResult
{
    private static readonly RelativeSyncPath Path = RelativeSyncPathFactory.Create("Photos/red car.jpg").Match(path => path, _ => throw new InvalidOperationException());

    [Fact]
    public void when_the_same_term_is_found_twice_then_only_the_first_is_kept()
    {
        var term = Term("red");
        var first = Candidate(term, SignalKindFactory.CreateVocabulary());
        var second = Candidate(term, SignalKindFactory.CreateFolderName());

        var result = FileClassificationResultFactory.Create(Path, [first, second]);

        result.Candidates.ShouldBe([first]);
    }

    [Fact]
    public void when_a_low_confidence_candidate_comes_first_then_the_high_confidence_one_for_the_same_term_wins()
    {
        var term = Term("china");
        var low = ClassificationCandidateFactory.Create(term, ClassificationCategoryFactory.Create(term.CategoryId, "Place"), SignalKindFactory.CreateExtension(), ClassificationConfidenceFactory.CreateLow());
        var high = Candidate(term, SignalKindFactory.CreateVocabulary());

        var result = FileClassificationResultFactory.Create(Path, [low, high]);

        result.Candidates.ShouldBe([high]);
    }

    [Fact]
    public void when_a_term_is_ignored_then_the_vocabulary_drops_it()
    {
        var active = Term("red");
        var ignored = ClassificationTermFactory.Create(TermIdFactory.Create(Guid.NewGuid()), "blue", CategoryIdFactory.Create(Guid.NewGuid()), TermSourceFactory.CreateUser(), TermStatusFactory.CreateIgnored())
            .Match(term => term, () => throw new InvalidOperationException());

        var vocabulary = ClassificationVocabularyFactory.Create([], [active, ignored]);

        vocabulary.Terms.ShouldBe([active]);
    }

    [Fact]
    public void when_no_candidates_are_found_then_the_result_has_none()
    {
        var result = FileClassificationResultFactory.Create(Path, []);

        result.Candidates.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_source_list_changes_later_then_the_result_is_unchanged()
    {
        var candidates = new List<ClassificationCandidate> { Candidate(Term("red"), SignalKindFactory.CreateVocabulary()) };
        var result = FileClassificationResultFactory.Create(Path, candidates);

        candidates.Clear();

        result.Candidates.Count.ShouldBe(1);
    }

    [Fact]
    public void when_vocabulary_is_created_then_both_lists_are_copied()
    {
        var terms = new List<ClassificationTerm> { Term("red") };
        var vocabulary = ClassificationVocabularyFactory.Create([], terms);

        terms.Clear();

        vocabulary.Terms.Count.ShouldBe(1);
    }

    private static ClassificationTerm Term(string text) =>
        ClassificationTermFactory.Create(TermIdFactory.Create(Guid.NewGuid()), text, CategoryIdFactory.Create(Guid.NewGuid()), TermSourceFactory.CreateSeed(), TermStatusFactory.CreateActive())
            .Match(term => term, () => throw new InvalidOperationException());

    private static ClassificationCandidate Candidate(ClassificationTerm term, SignalKind signal) =>
        ClassificationCandidateFactory.Create(term, ClassificationCategoryFactory.Create(term.CategoryId, "Colour"), signal, ClassificationConfidenceFactory.CreateHigh());
}
