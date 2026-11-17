using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAClassificationVocabularyProvider
{
    [Fact]
    public async Task when_called_many_times_then_the_stores_are_read_once()
    {
        var terms = new CountingTermStore { Terms = TermsOf(Term("red", Colour)) };
        var categories = new CountingCategoryStore();
        var provider = new ClassificationVocabularyProvider(terms, categories);

        for (var call = 0; call < 5; call++)
        {
            await provider.GetAsync(TestContext.Current.CancellationToken);
        }

        (terms.GetAllCalls, categories.GetAllCalls).ShouldBe((1, 1));
    }

    [Fact]
    public async Task when_loaded_then_the_vocabulary_holds_the_stored_terms_and_categories()
    {
        var provider = new ClassificationVocabularyProvider(new CountingTermStore { Terms = TermsOf(Term("red", Colour)) }, new CountingCategoryStore());

        var result = await provider.GetAsync(TestContext.Current.CancellationToken);

        var vocabulary = await result.MatchAsync(found => found, _ => throw new InvalidOperationException());
        vocabulary.Terms.Select(term => term.Text).ShouldBe(["red"]);
        vocabulary.Categories.Select(category => category.Name).ShouldBe(["Colour"]);
    }

    [Fact]
    public void when_invalidated_then_the_generation_increases_each_time()
    {
        var provider = new ClassificationVocabularyProvider(new CountingTermStore(), new CountingCategoryStore());
        var before = provider.Generation;

        provider.Invalidate();
        provider.Invalidate();

        provider.Generation.ShouldBe(before + 2);
    }

    [Fact]
    public async Task when_loaded_without_invalidation_then_the_generation_is_unchanged()
    {
        var provider = new ClassificationVocabularyProvider(new CountingTermStore(), new CountingCategoryStore());
        var before = provider.Generation;

        await provider.GetAsync(TestContext.Current.CancellationToken);

        provider.Generation.ShouldBe(before);
    }

    [Fact]
    public async Task when_invalidated_then_the_next_call_reloads_new_data()
    {
        var terms = new CountingTermStore { Terms = TermsOf(Term("red", Colour)) };
        var provider = new ClassificationVocabularyProvider(terms, new CountingCategoryStore());
        await provider.GetAsync(TestContext.Current.CancellationToken);
        terms.Terms = TermsOf(Term("blue", Colour));

        provider.Invalidate();
        var result = await provider.GetAsync(TestContext.Current.CancellationToken);

        var vocabulary = await result.MatchAsync(found => found, _ => throw new InvalidOperationException());
        terms.GetAllCalls.ShouldBe(2);
        vocabulary.Terms.Select(term => term.Text).ShouldBe(["blue"]);
    }

    [Fact]
    public async Task when_invalidated_during_a_load_then_the_stale_result_is_not_cached()
    {
        var gate = new TaskCompletionSource();
        var terms = new CountingTermStore { Terms = TermsOf(Term("red", Colour)), Gate = gate.Task };
        var provider = new ClassificationVocabularyProvider(terms, new CountingCategoryStore());
        var staleLoad = provider.GetAsync(TestContext.Current.CancellationToken);
        await terms.Entered.Task;
        terms.Terms = TermsOf(Term("blue", Colour));
        provider.Invalidate();

        gate.SetResult();
        await staleLoad;
        var result = await provider.GetAsync(TestContext.Current.CancellationToken);

        var vocabulary = await result.MatchAsync(found => found, _ => throw new InvalidOperationException());
        terms.GetAllCalls.ShouldBe(2);
        vocabulary.Terms.Select(term => term.Text).ShouldBe(["blue"]);
    }

    [Fact]
    public async Task when_many_callers_arrive_together_then_they_share_one_load()
    {
        var gate = new TaskCompletionSource();
        var terms = new CountingTermStore { Terms = TermsOf(Term("red", Colour)), Gate = gate.Task };
        var provider = new ClassificationVocabularyProvider(terms, new CountingCategoryStore());
        var callers = Enumerable.Range(0, 20).Select(_ => Task.Run(() => provider.GetAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken)).ToArray();
        await terms.Entered.Task;
        await Task.Delay(100, TestContext.Current.CancellationToken);

        gate.SetResult();
        var results = await Task.WhenAll(callers);

        (terms.GetAllCalls, results.Distinct().Count()).ShouldBe((1, 1));
    }

    [Fact]
    public async Task when_the_term_store_fails_then_the_failure_is_not_cached()
    {
        var terms = new CountingTermStore { Terms = new Fail<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()) };
        var provider = new ClassificationVocabularyProvider(terms, new CountingCategoryStore());
        var first = await provider.GetAsync(TestContext.Current.CancellationToken);
        terms.Terms = TermsOf(Term("red", Colour));

        var second = await provider.GetAsync(TestContext.Current.CancellationToken);

        (first, second).ShouldSatisfyAllConditions(
            () => first.ShouldBeOfType<Fail<ClassificationVocabulary, ClassificationFailure>>(),
            () => second.ShouldBeOfType<Ok<ClassificationVocabulary, ClassificationFailure>>(),
            () => terms.GetAllCalls.ShouldBe(2));
    }

    [Fact]
    public async Task when_the_category_store_fails_then_the_failure_is_not_cached()
    {
        var categories = new CountingCategoryStore { Categories = new Fail<IReadOnlyList<ClassificationCategory>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()) };
        var provider = new ClassificationVocabularyProvider(new CountingTermStore(), categories);
        var first = await provider.GetAsync(TestContext.Current.CancellationToken);
        categories.Categories = new Ok<IReadOnlyList<ClassificationCategory>, ClassificationFailure>([]);

        var second = await provider.GetAsync(TestContext.Current.CancellationToken);

        (first, second).ShouldSatisfyAllConditions(
            () => first.ShouldBeOfType<Fail<ClassificationVocabulary, ClassificationFailure>>(),
            () => second.ShouldBeOfType<Ok<ClassificationVocabulary, ClassificationFailure>>(),
            () => categories.GetAllCalls.ShouldBe(2));
    }

    [Fact]
    public async Task when_the_caller_passes_a_token_then_the_store_receives_it()
    {
        using var source = new CancellationTokenSource();
        var terms = new CountingTermStore();
        var provider = new ClassificationVocabularyProvider(terms, new CountingCategoryStore());

        await provider.GetAsync(source.Token);

        terms.LastToken.ShouldBe(source.Token);
    }

    [Fact]
    public async Task when_the_caller_is_already_cancelled_then_it_throws_and_loads_nothing()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        var terms = new CountingTermStore();
        var provider = new ClassificationVocabularyProvider(terms, new CountingCategoryStore());

        await Should.ThrowAsync<OperationCanceledException>(() => provider.GetAsync(source.Token));

        terms.GetAllCalls.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_caller_that_started_the_load_cancels_then_another_caller_still_gets_a_vocabulary()
    {
        var gate = new TaskCompletionSource();
        var terms = new CountingTermStore { Terms = TermsOf(Term("red", Colour)), Gate = gate.Task };
        var provider = new ClassificationVocabularyProvider(terms, new CountingCategoryStore());
        using var starter = new CancellationTokenSource();
        var cancelled = provider.GetAsync(starter.Token);
        await terms.Entered.Task;
        var survivor = provider.GetAsync(TestContext.Current.CancellationToken);

        await starter.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => cancelled);
        gate.SetResult();
        var result = await survivor;

        result.ShouldBeOfType<Ok<ClassificationVocabulary, ClassificationFailure>>();
        terms.GetAllCalls.ShouldBe(2);
    }

    private static Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure> TermsOf(params ClassificationTerm[] terms) => new Ok<IReadOnlyList<ClassificationTerm>, ClassificationFailure>(terms);
}
