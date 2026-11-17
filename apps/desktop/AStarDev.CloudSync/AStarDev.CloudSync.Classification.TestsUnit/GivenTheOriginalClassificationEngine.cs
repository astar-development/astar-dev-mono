using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenTheOriginalClassificationEngine
{
    private static readonly ClassificationTerm[] HandPickedTerms =
    [
        Term("red", Colour), Term("red car", Place), Term("red car park", Place), Term("car park", Colour), Term("car", Place), Term("china", Colour), Term("china", Place),
        Term("new york", Place), Term("los angeles", Place), Term("são paulo", Place), Term("café", Place), Term("box", Colour), Term("holiday", Place), Term("123", Colour),
        Term("news", Place), Term("bus", Place), Term("movies", Place), Term("東京", Place), Term("straße", Place), Term("mp3", Colour), Term("4k", Colour)
    ];

    [Fact]
    public void when_random_text_is_tokenised_then_the_tokens_equal_the_original()
    {
        foreach (var text in EquivalenceCorpus.Texts(1889, []).Append(null))
        {
            ClassificationTokeniser.Tokenise(text).ShouldBe(OriginalClassificationTokeniser.Tokenise(text), $"text: {text}");
        }
    }

    [Fact]
    public void when_random_tokens_are_matched_then_the_terms_equal_the_original()
    {
        var original = new OriginalClassificationEngine(HandPickedTerms);
        var index = PhraseIndexFactory.Create(HandPickedTerms);
        var vocabularyTokens = HandPickedTerms.SelectMany(term => ClassificationTokeniser.Tokenise(term.Text)).Distinct().ToList();
        var random = new Random(1889);

        for (var attempt = 0; attempt < EquivalenceCorpus.Cases; attempt++)
        {
            var tokens = EquivalenceCorpus.Tokens(random, vocabularyTokens);

            PhraseMatcher.Match(tokens, index).ShouldBe(original.Match(tokens), $"tokens: {string.Join('|', tokens)}");
        }
    }

    [Fact]
    public async Task when_random_paths_are_evaluated_by_the_folder_signal_then_the_terms_equal_the_original()
    {
        var (vocabulary, fragments) = await SeedVocabularyAsync();
        var original = new OriginalClassificationEngine(vocabulary.Terms);
        var signal = new FolderSegmentSignal();

        foreach (var path in ValidPaths(1889, fragments))
        {
            signal.Evaluate(path, vocabulary).Select(candidate => candidate.Term).ShouldBe(original.FolderTerms(path.Value), $"path: {path.Value}");
        }
    }

    [Fact]
    public async Task when_random_paths_are_evaluated_by_the_dictionary_signal_then_the_terms_equal_the_original()
    {
        var (vocabulary, fragments) = await SeedVocabularyAsync();
        var original = new OriginalClassificationEngine(vocabulary.Terms);
        var signal = new DictionaryTermSignal();

        foreach (var path in ValidPaths(1890, fragments))
        {
            signal.Evaluate(path, vocabulary).Select(candidate => candidate.Term).ShouldBe(original.FileNameTerms(path.Value), $"path: {path.Value}");
        }
    }

    private static IEnumerable<RelativeSyncPath> ValidPaths(int seed, IReadOnlyList<string> fragments) =>
        EquivalenceCorpus.Paths(seed, fragments).SelectMany(path => RelativeSyncPathFactory.Create(path).Match(created => (IEnumerable<RelativeSyncPath>)[created], _ => []));

    private static async Task<(ClassificationVocabulary Vocabulary, IReadOnlyList<string> Fragments)> SeedVocabularyAsync()
    {
        var seed = await EmbeddedClassificationSeedSourceFactory.CreateDefault().LoadAsync(TestContext.Current.CancellationToken).MatchAsync(loaded => loaded, _ => throw new InvalidOperationException());
        var categories = seed.Categories.Select(category => ClassificationCategoryFactory.Create(CategoryIdFactory.Create(Guid.NewGuid()), category.Name)).ToList();
        var categoryIds = categories.ToDictionary(category => category.Name, category => category.Id, StringComparer.Ordinal);
        var terms = seed.Terms.Select(term => Term(term.Text, categoryIds[term.CategoryName])).Concat(HandPickedTerms.Select(term => Term(term.Text, categoryIds[seed.Categories[0].Name]))).ToList();
        var multiWordTexts = seed.Terms.Select(term => term.Text).Where(text => text.Contains(' ', StringComparison.Ordinal)).Take(400);

        return (ClassificationVocabularyFactory.Create(categories, terms), [.. multiWordTexts.Concat(seed.Terms.Select(term => term.Text).Take(400))]);
    }
}
