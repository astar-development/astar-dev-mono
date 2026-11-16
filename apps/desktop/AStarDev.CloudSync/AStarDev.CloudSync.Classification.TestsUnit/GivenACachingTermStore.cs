using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenACachingTermStore
{
    private static Result<ClassificationVocabulary, ClassificationFailure> Empty => new Ok<ClassificationVocabulary, ClassificationFailure>(Vocabulary());

    [Fact]
    public async Task when_a_term_is_upserted_then_the_vocabulary_is_invalidated()
    {
        var provider = new RecordingVocabularyProvider(Empty);
        var store = new CachingTermStore(new CountingTermStore(), provider);

        await store.UpsertAsync(Term("red", Colour), TestContext.Current.CancellationToken);

        provider.Invalidations.ShouldBe(1);
    }

    [Fact]
    public async Task when_many_terms_are_upserted_then_the_vocabulary_is_invalidated_once()
    {
        var terms = new[] { Term("red", Colour), Term("blue", Colour), Term("green", Colour) };
        var provider = new RecordingVocabularyProvider(Empty);
        var store = new CachingTermStore(new CountingTermStore { WriteManyResult = new Ok<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(terms) }, provider);

        var result = await store.UpsertManyAsync(terms, TestContext.Current.CancellationToken);

        (provider.Invalidations, result).ShouldBe((1, new Ok<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(terms)));
    }

    [Fact]
    public async Task when_a_batch_fails_then_the_vocabulary_is_not_invalidated_and_the_failure_is_returned()
    {
        var failure = new Fail<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable());
        var provider = new RecordingVocabularyProvider(Empty);
        var store = new CachingTermStore(new CountingTermStore { WriteManyResult = failure }, provider);

        var result = await store.UpsertManyAsync([Term("red", Colour)], TestContext.Current.CancellationToken);

        (provider.Invalidations, result).ShouldBe((0, failure));
    }

    [Fact]
    public async Task when_a_status_is_set_then_the_vocabulary_is_invalidated()
    {
        var provider = new RecordingVocabularyProvider(Empty);
        var store = new CachingTermStore(new CountingTermStore(), provider);

        await store.SetStatusAsync(TermIdFactory.Create(Guid.NewGuid()), TermStatusFactory.CreateIgnored(), TestContext.Current.CancellationToken);

        provider.Invalidations.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_write_fails_then_the_vocabulary_is_not_invalidated_and_the_failure_is_returned()
    {
        var failure = new Fail<ClassificationTerm, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable());
        var provider = new RecordingVocabularyProvider(Empty);
        var store = new CachingTermStore(new CountingTermStore { WriteResult = failure }, provider);

        var upserted = await store.UpsertAsync(Term("red", Colour), TestContext.Current.CancellationToken);
        var statusSet = await store.SetStatusAsync(TermIdFactory.Create(Guid.NewGuid()), TermStatusFactory.CreateIgnored(), TestContext.Current.CancellationToken);

        (provider.Invalidations, upserted, statusSet).ShouldBe((0, failure, failure));
    }

    [Fact]
    public async Task when_a_write_succeeds_then_the_inner_result_is_returned()
    {
        var term = Term("red", Colour);
        var store = new CachingTermStore(new CountingTermStore { WriteResult = new Ok<ClassificationTerm, ClassificationFailure>(term) }, new RecordingVocabularyProvider(Empty));

        var result = await store.UpsertAsync(term, TestContext.Current.CancellationToken);

        result.ShouldBe(new Ok<ClassificationTerm, ClassificationFailure>(term));
    }

    [Fact]
    public async Task when_terms_are_read_then_nothing_is_invalidated()
    {
        var provider = new RecordingVocabularyProvider(Empty);
        var inner = new CountingTermStore { Terms = new Ok<IReadOnlyList<ClassificationTerm>, ClassificationFailure>([Term("red", Colour)]) };
        var store = new CachingTermStore(inner, provider);

        var all = await store.GetAllAsync(TestContext.Current.CancellationToken);
        var byId = await store.GetByIdAsync(TermIdFactory.Create(Guid.NewGuid()), TestContext.Current.CancellationToken);

        (provider.Invalidations, inner.GetAllCalls, all.ShouldBeOfType<Ok<IReadOnlyList<ClassificationTerm>, ClassificationFailure>>().Value.Count, byId).ShouldBe((0, 1, 1, byId));
    }

    [Fact]
    public async Task when_a_write_lands_between_provider_loads_then_the_provider_serves_the_new_terms()
    {
        var inner = new CountingTermStore { Terms = new Ok<IReadOnlyList<ClassificationTerm>, ClassificationFailure>([Term("red", Colour)]) };
        var provider = new ClassificationVocabularyProvider(inner, new CountingCategoryStore());
        var store = new CachingTermStore(inner, provider);
        await provider.GetAsync(TestContext.Current.CancellationToken);
        inner.Terms = new Ok<IReadOnlyList<ClassificationTerm>, ClassificationFailure>([Term("blue", Colour)]);

        await store.UpsertAsync(Term("blue", Colour), TestContext.Current.CancellationToken);
        var result = await provider.GetAsync(TestContext.Current.CancellationToken);

        var vocabulary = await result.MatchAsync(found => found, _ => throw new InvalidOperationException());
        vocabulary.Terms.Select(term => term.Text).ShouldBe(["blue"]);
    }
}
