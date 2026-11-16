using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAFolderSegmentSignal
{
    private readonly FolderSegmentSignal signal = new();

    [Fact]
    public void when_a_folder_name_holds_a_term_then_a_folder_candidate_is_returned()
    {
        var candidates = signal.Evaluate(PathOf("Holidays/New York 2019/img001.jpg"), Vocabulary(Term("new york", Place), Term("holiday", Place)));

        Texts(candidates).ShouldBe(["holiday", "new york"]);
        candidates.ShouldAllBe(candidate => candidate.Signal is SignalKind.FolderName);
    }

    [Fact]
    public void when_the_file_name_holds_a_term_then_it_is_not_returned() =>
        signal.Evaluate(PathOf("Photos/red.jpg"), Vocabulary(Term("red", Colour))).ShouldBeEmpty();

    [Fact]
    public void when_the_folder_casing_differs_then_it_still_matches() =>
        signal.Evaluate(PathOf("RED/x.jpg"), Vocabulary(Term("red", Colour))).Count.ShouldBe(1);

    [Fact]
    public void when_the_path_has_no_folder_then_nothing_is_returned() =>
        signal.Evaluate(PathOf("red.jpg"), Vocabulary(Term("red", Colour))).ShouldBeEmpty();
}
