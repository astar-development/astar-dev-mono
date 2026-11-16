using AStarDev.CloudSync.Sync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAddClassification
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private readonly CountingTermStore terms = new();
    private readonly CountingCategoryStore categories = new();

    private ServiceCollection Persistence() =>
    [
        ServiceDescriptor.Singleton<ITermStore>(terms),
        ServiceDescriptor.Singleton<ICategoryStore>(categories),
        ServiceDescriptor.Singleton<ISyncBaselineWriter>(new ClassifyHookInnerWriter()),
        ServiceDescriptor.Singleton<IUploadRecorder>(new UploadHookInnerRecorder())
    ];

    [Fact]
    public async Task when_a_term_is_written_through_the_decorated_store_then_the_next_vocabulary_read_reloads()
    {
        var services = Persistence();
        await using var provider = services.AddClassification().BuildServiceProvider();
        var vocabulary = provider.GetRequiredService<IClassificationVocabularyProvider>();
        await vocabulary.GetAsync(CancellationToken);
        await vocabulary.GetAsync(CancellationToken);
        var cached = terms.GetAllCalls;

        await provider.GetRequiredService<ITermStore>().UpsertAsync(Term("blue", Colour), CancellationToken);
        await vocabulary.GetAsync(CancellationToken);

        (cached, terms.GetAllCalls).ShouldBe((1, 2));
    }

    [Fact]
    public async Task when_a_category_is_written_through_the_decorated_store_then_the_next_vocabulary_read_reloads()
    {
        var services = Persistence();
        await using var provider = services.AddClassification().BuildServiceProvider();
        var vocabulary = provider.GetRequiredService<IClassificationVocabularyProvider>();
        await vocabulary.GetAsync(CancellationToken);
        await vocabulary.GetAsync(CancellationToken);
        var cached = categories.GetAllCalls;

        await provider.GetRequiredService<ICategoryStore>().UpsertAsync(ClassificationCategoryFactory.Create(Place, "Place"), CancellationToken);
        await vocabulary.GetAsync(CancellationToken);

        (cached, categories.GetAllCalls).ShouldBe((1, 2));
    }

    [Theory]
    [InlineData(typeof(ITermStore))]
    [InlineData(typeof(ICategoryStore))]
    [InlineData(typeof(ISyncBaselineWriter))]
    [InlineData(typeof(IUploadRecorder))]
    public void when_a_registration_to_wrap_is_missing_then_a_clear_invalid_operation_is_thrown_at_composition(Type missing)
    {
        var services = Persistence();
        services.RemoveAll(missing);

        var exception = Should.Throw<InvalidOperationException>(() => services.AddClassification());

        exception.Message.ShouldContain(missing.Name);
    }

    [Fact]
    public void when_classification_is_added_to_an_empty_collection_then_a_clear_invalid_operation_is_thrown()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() => services.AddClassification());
    }

    [Fact]
    public void when_classification_is_added_twice_then_a_clear_invalid_operation_is_thrown_and_the_first_registration_is_untouched()
    {
        var services = Persistence();
        services.AddClassification();
        var count = services.Count;

        var exception = Should.Throw<InvalidOperationException>(() => services.AddClassification());

        (exception.Message.Contains("already", StringComparison.OrdinalIgnoreCase), services.Count).ShouldBe((true, count));
    }
}
