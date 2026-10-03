using AStarDev.ScraperPlaying.Scoping;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationUpdater : IDisposable
{
    private readonly FakeRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository;
    private readonly ServiceProvider serviceProvider;
    private readonly ScrapeConfigurationUpdater updater;
    private readonly FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero));

    public GivenAScrapeConfigurationUpdater()
    {
        var unitOfWork = new FakeUnitOfWork();
        repository = unitOfWork.Register<ScrapeConfigurationEntity, ScrapeConfigurationId>();
        serviceProvider = new ServiceCollection().AddScoped<IUnitOfWork>(_ => unitOfWork).BuildServiceProvider();
        updater = new ScrapeConfigurationUpdater(new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()), timeProvider);
    }

    public void Dispose() => serviceProvider.Dispose();

    [Fact]
    public async Task when_a_search_edit_is_saved_then_the_modified_time_is_the_time_provider_time()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration();
        repository.Found = (Option<ScrapeConfigurationEntity>)configuration;

        _ = await updater.SaveAsync(configuration.Id, [new SearchSettings("dogs", Option.None<int>())], TestContext.Current.CancellationToken);

        configuration.SearchConfiguration.UpdatedAt.ShouldBe(timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task when_the_token_is_cancelled_then_the_save_is_cancelled()
    {
        repository.Found = (Option<ScrapeConfigurationEntity>)ScrapeConfigurationTestData.CreateConfiguration();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => updater.SaveAsync(new ScrapeConfigurationId(Guid.CreateVersion7()), [], cancellation.Token));
    }
}
