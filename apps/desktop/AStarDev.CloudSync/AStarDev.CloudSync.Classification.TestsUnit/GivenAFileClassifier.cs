using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAFileClassifier
{
    private static readonly IClassificationSignal[] Signals = [new ExtensionSignal(), new FolderSegmentSignal(), new DictionaryTermSignal()];

    [Fact]
    public async Task when_the_path_matches_several_signals_then_all_terms_are_merged()
    {
        var terms = new[] { Term("jpg", Colour), Term("holiday", Place), Term("red", Colour), Term("ignored", Colour, TermSourceFactory.CreateSeed(), TermStatusFactory.CreateIgnored()) };
        var classifier = Classifier(terms);

        var result = await classifier.ClassifyAsync(PathOf("Holidays/ignored red.jpg"), TestContext.Current.CancellationToken);

        Texts(result.Candidates).Order().ShouldBe(["holiday", "jpg", "red"]);
    }

    [Fact]
    public async Task when_nothing_matches_then_the_file_is_unclassified()
    {
        var classifier = Classifier(Term("red", Colour));

        var result = await classifier.ClassifyAsync(PathOf("misc/xyz.bin"), TestContext.Current.CancellationToken);

        result.Candidates.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_term_store_fails_then_the_file_is_unclassified()
    {
        var classifier = new FileClassifier(Signals, new ClassificationVocabularyProvider(new FakeTermStore(new Fail<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable())), new FakeCategoryStore(new Ok<IReadOnlyList<ClassificationCategory>, ClassificationFailure>([]))));

        var result = await classifier.ClassifyAsync(PathOf("red.jpg"), TestContext.Current.CancellationToken);

        result.Candidates.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_category_store_fails_then_the_file_is_unclassified()
    {
        var classifier = new FileClassifier(Signals, new ClassificationVocabularyProvider(new FakeTermStore(new Ok<IReadOnlyList<ClassificationTerm>, ClassificationFailure>([Term("red", Colour)])), new FakeCategoryStore(new Fail<IReadOnlyList<ClassificationCategory>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()))));

        var result = await classifier.ClassifyAsync(PathOf("red.jpg"), TestContext.Current.CancellationToken);

        result.Candidates.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_many_files_are_classified_then_the_stores_are_read_once()
    {
        var terms = new CountingTermStore { Terms = new Ok<IReadOnlyList<ClassificationTerm>, ClassificationFailure>([Term("red", Colour)]) };
        var classifier = new FileClassifier(Signals, new ClassificationVocabularyProvider(terms, new CountingCategoryStore()));

        for (var file = 0; file < 10; file++)
        {
            await classifier.ClassifyAsync(PathOf($"red{file}.jpg"), TestContext.Current.CancellationToken);
        }

        terms.GetAllCalls.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_store_recovers_after_a_failure_then_the_next_file_is_classified()
    {
        var terms = new CountingTermStore { Terms = new Fail<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()) };
        var classifier = new FileClassifier(Signals, new ClassificationVocabularyProvider(terms, new CountingCategoryStore()));
        await classifier.ClassifyAsync(PathOf("red.jpg"), TestContext.Current.CancellationToken);
        terms.Terms = new Ok<IReadOnlyList<ClassificationTerm>, ClassificationFailure>([Term("red", Colour)]);

        var result = await classifier.ClassifyAsync(PathOf("red.jpg"), TestContext.Current.CancellationToken);

        Texts(result.Candidates).ShouldBe(["red"]);
    }

    private static FileClassifier Classifier(params ClassificationTerm[] terms) =>
        new(Signals, new ClassificationVocabularyProvider(new FakeTermStore(new Ok<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(terms)), new FakeCategoryStore(new Ok<IReadOnlyList<ClassificationCategory>, ClassificationFailure>([ClassificationCategoryFactory.Create(Colour, "Colour"), ClassificationCategoryFactory.Create(Place, "Place")]))));

    private sealed class FakeTermStore(Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure> all) : ITermStore
    {
        public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult(all);

        public Task<Result<ClassificationTerm, ClassificationFailure>> UpsertAsync(ClassificationTerm term, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> UpsertManyAsync(IReadOnlyList<ClassificationTerm> terms, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Result<Option<ClassificationTerm>, ClassificationFailure>> GetByIdAsync(TermId termId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Result<ClassificationTerm, ClassificationFailure>> SetStatusAsync(TermId termId, TermStatus status, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeCategoryStore(Result<IReadOnlyList<ClassificationCategory>, ClassificationFailure> all) : ICategoryStore
    {
        public Task<Result<IReadOnlyList<ClassificationCategory>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult(all);

        public Task<Result<ClassificationCategory, ClassificationFailure>> UpsertAsync(ClassificationCategory category, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
