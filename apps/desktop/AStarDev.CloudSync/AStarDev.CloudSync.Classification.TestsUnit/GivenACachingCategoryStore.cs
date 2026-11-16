using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenACachingCategoryStore
{
    private static Result<ClassificationVocabulary, ClassificationFailure> Empty => new Ok<ClassificationVocabulary, ClassificationFailure>(Vocabulary());

    [Fact]
    public async Task when_a_category_is_upserted_then_the_vocabulary_is_invalidated()
    {
        var provider = new RecordingVocabularyProvider(Empty);
        var store = new CachingCategoryStore(new CountingCategoryStore(), provider);

        await store.UpsertAsync(ClassificationCategoryFactory.Create(Place, "Place"), TestContext.Current.CancellationToken);

        provider.Invalidations.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_upsert_fails_then_the_vocabulary_is_not_invalidated_and_the_failure_is_returned()
    {
        var failure = new Fail<ClassificationCategory, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable());
        var provider = new RecordingVocabularyProvider(Empty);
        var store = new CachingCategoryStore(new CountingCategoryStore { WriteResult = failure }, provider);

        var result = await store.UpsertAsync(ClassificationCategoryFactory.Create(Place, "Place"), TestContext.Current.CancellationToken);

        (provider.Invalidations, result).ShouldBe((0, failure));
    }

    [Fact]
    public async Task when_categories_are_read_then_the_inner_result_is_returned_and_nothing_is_invalidated()
    {
        var provider = new RecordingVocabularyProvider(Empty);
        var inner = new CountingCategoryStore();
        var store = new CachingCategoryStore(inner, provider);

        var result = await store.GetAllAsync(TestContext.Current.CancellationToken);

        (provider.Invalidations, inner.GetAllCalls, result).ShouldBe((0, 1, inner.Categories));
    }

    [Fact]
    public async Task when_a_category_is_upserted_then_the_provider_serves_the_new_categories()
    {
        var inner = new CountingCategoryStore();
        var provider = new ClassificationVocabularyProvider(new CountingTermStore(), inner);
        var store = new CachingCategoryStore(inner, provider);
        await provider.GetAsync(TestContext.Current.CancellationToken);
        inner.Categories = new Ok<IReadOnlyList<ClassificationCategory>, ClassificationFailure>([ClassificationCategoryFactory.Create(Place, "Place")]);

        await store.UpsertAsync(ClassificationCategoryFactory.Create(Place, "Place"), TestContext.Current.CancellationToken);
        var result = await provider.GetAsync(TestContext.Current.CancellationToken);

        var vocabulary = await result.MatchAsync(found => found, _ => throw new InvalidOperationException());
        vocabulary.Categories.Select(category => category.Name).ShouldBe(["Place"]);
    }
}
