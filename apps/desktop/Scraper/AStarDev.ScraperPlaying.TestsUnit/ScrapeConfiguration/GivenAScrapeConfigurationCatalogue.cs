using AStarDev.ScraperPlaying.Scoping;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationCatalogue : IDisposable
{
    private readonly FakeRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository;
    private readonly FakeScrapeConfigurationLookup lookup = new();
    private readonly ServiceProvider serviceProvider;
    private readonly ScrapeConfigurationCatalogue catalogue;

    public GivenAScrapeConfigurationCatalogue()
    {
        var unitOfWork = new FakeUnitOfWork();
        repository = unitOfWork.Register<ScrapeConfigurationEntity, ScrapeConfigurationId>();
        serviceProvider = new ServiceCollection().AddScoped<IUnitOfWork>(_ => unitOfWork).AddScoped<IScrapeConfigurationLookup>(_ => lookup).BuildServiceProvider();
        catalogue = new ScrapeConfigurationCatalogue(new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()));
    }

    [Fact]
    public async Task when_configurations_exist_then_a_summary_is_listed_for_each()
    {
        var first = new ScrapeConfigurationHeader(new ScrapeConfigurationId(Guid.CreateVersion7()), new Uri("https://example.com"), "cats");
        var second = new ScrapeConfigurationHeader(new ScrapeConfigurationId(Guid.CreateVersion7()), new Uri("https://example.com"), "dogs");
        lookup.Headers = Exceptional.Success<IReadOnlyList<ScrapeConfigurationHeader>>([first, second]);

        var result = await catalogue.ListAsync(TestContext.Current.CancellationToken);

        var summaries = result.Match(list => list, exception => throw exception);
        (string.Join(",", summaries.Select(summary => summary.Id.Value)), string.Join(",", summaries.Select(summary => summary.Label))).ShouldBe(($"{first.Id.Value},{second.Id.Value}", "example.com - cats,example.com - dogs"));
    }

    [Fact]
    public async Task when_the_token_is_cancelled_then_listing_is_cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => catalogue.ListAsync(cancellation.Token));
    }

    [Fact]
    public async Task when_the_token_is_cancelled_then_finding_is_cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => catalogue.FindAsync(new ScrapeConfigurationId(Guid.CreateVersion7()), cancellation.Token));
    }

    [Fact]
    public async Task when_no_configurations_exist_then_the_list_is_empty()
    {
        lookup.Headers = Exceptional.Success<IReadOnlyList<ScrapeConfigurationHeader>>([]);

        var result = await catalogue.ListAsync(TestContext.Current.CancellationToken);

        result.Match(list => list, exception => throw exception).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_listing_fails_then_the_failure_is_returned()
    {
        var failure = new InvalidOperationException("list failed");
        lookup.Headers = failure;

        var result = await catalogue.ListAsync(TestContext.Current.CancellationToken);

        result.Match(_ => (Exception?)null, exception => exception).ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task when_a_configuration_is_found_then_it_is_returned()
    {
        var entity = CreateEntity("cats");
        repository.Found = (Option<ScrapeConfigurationEntity>)entity;

        var result = await catalogue.FindAsync(entity.Id, TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(found => found.Id, () => default).ShouldBe(entity.Id);
    }

    [Fact]
    public async Task when_a_configuration_is_not_found_then_none_is_returned()
    {
        repository.Found = Option<ScrapeConfigurationEntity>.None.Instance;

        var result = await catalogue.FindAsync(new ScrapeConfigurationId(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeFalse();
    }

    [Fact]
    public async Task when_finding_fails_then_the_failure_is_returned()
    {
        var failure = new InvalidOperationException("find failed");
        repository.Found = failure;

        var result = await catalogue.FindAsync(new ScrapeConfigurationId(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Match(_ => (Exception?)null, exception => exception).ShouldBeSameAs(failure);
    }

    public void Dispose() => serviceProvider.Dispose();

    private static Exceptional<Option<T>> Success<T>(T value) => (Option<T>)value;

    private static ScrapeConfigurationEntity CreateEntity(string searchTerm)
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());

        return new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            SearchConfiguration = new SearchConfigurationEntity(new SearchConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, searchTerm, 10, [])
        };
    }
}
