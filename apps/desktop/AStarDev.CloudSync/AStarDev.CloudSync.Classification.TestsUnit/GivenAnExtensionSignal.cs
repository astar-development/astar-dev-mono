using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAnExtensionSignal
{
    private readonly ExtensionSignal signal = new();

    [Theory]
    [InlineData("jpg")]
    [InlineData(".jpg")]
    public void when_the_extension_matches_a_term_then_a_high_extension_candidate_is_returned(string termText)
    {
        var candidates = signal.Evaluate(PathOf("Photos/holiday.JPG"), Vocabulary(Term(termText, Colour)));

        candidates.Count.ShouldBe(1);
        candidates[0].Signal.ShouldBeOfType<SignalKind.Extension>();
        candidates[0].Confidence.ShouldBeOfType<ClassificationConfidence.High>();
    }

    [Fact]
    public void when_the_file_has_no_extension_then_nothing_is_returned() =>
        signal.Evaluate(PathOf("Photos/holiday"), Vocabulary(Term("jpg", Colour))).ShouldBeEmpty();

    [Fact]
    public void when_a_folder_is_named_like_the_extension_then_nothing_is_returned() =>
        signal.Evaluate(PathOf("jpg/holiday.png"), Vocabulary(Term("jpg", Colour))).ShouldBeEmpty();

    [Fact]
    public void when_the_term_category_is_unknown_then_nothing_is_returned() =>
        signal.Evaluate(PathOf("a.jpg"), Vocabulary(Term("jpg", CategoryIdFactory.Create(Guid.NewGuid())))).ShouldBeEmpty();

    [Fact]
    public void when_the_term_is_suggested_then_the_confidence_is_low() =>
        signal.Evaluate(PathOf("a.jpg"), Vocabulary(Term("jpg", Colour, TermSourceFactory.CreateSuggested(), TermStatusFactory.CreateActive())))[0].Confidence.ShouldBeOfType<ClassificationConfidence.Low>();

    [Fact]
    public void when_a_large_vocabulary_is_evaluated_repeatedly_then_the_lookup_does_not_scan_every_term()
    {
        var terms = Enumerable.Range(0, 100_000).Select(index => Term($"term{index}", Colour)).Append(Term(".jpg", Colour)).ToArray();
        var vocabulary = Vocabulary(terms);
        var path = PathOf("Photos/holiday.jpg");
        _ = signal.Evaluate(path, vocabulary);
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var counts = Enumerable.Range(0, 2000).Select(_ => signal.Evaluate(path, vocabulary).Count).ToArray();

        watch.Stop();
        counts.ShouldAllBe(count => count == 1);
        watch.ElapsedMilliseconds.ShouldBeLessThan(500);
    }
}
