using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenADictionaryTermSignal
{
    private readonly DictionaryTermSignal signal = new();

    [Fact]
    public void when_the_file_name_holds_a_phrase_then_the_longest_phrase_is_returned()
    {
        var candidates = signal.Evaluate(PathOf("Photos/Red_Car_2020.jpg"), Vocabulary(Term("red", Colour), Term("red car", Place)));

        Texts(candidates).ShouldBe(["red car"]);
        candidates[0].Signal.ShouldBeOfType<SignalKind.Vocabulary>();
    }

    [Fact]
    public void when_the_file_name_uses_camel_case_then_the_words_match() =>
        Texts(signal.Evaluate(PathOf("RedCar.png"), Vocabulary(Term("car", Place)))).ShouldBe(["car"]);

    [Fact]
    public void when_only_the_extension_or_folder_holds_the_term_then_nothing_is_returned()
    {
        signal.Evaluate(PathOf("red/photo.car"), Vocabulary(Term("red", Colour), Term("car", Place))).ShouldBeEmpty();
    }

    [Fact]
    public void when_the_term_is_suggested_then_the_confidence_is_low() =>
        signal.Evaluate(PathOf("red.jpg"), Vocabulary(Term("red", Colour, TermSourceFactory.CreateSuggested(), TermStatusFactory.CreateActive())))[0].Confidence.ShouldBeOfType<ClassificationConfidence.Low>();

    [Fact]
    public void when_the_vocabulary_is_empty_then_nothing_is_returned() =>
        signal.Evaluate(PathOf("red.jpg"), Vocabulary()).ShouldBeEmpty();
}
