using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.TestsUnit.Fakes;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenScrapeConfigurationLoading
{
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository;

    public GivenScrapeConfigurationLoading() => repository = unitOfWork.Register<ScrapeConfigurationEntity, ScrapeConfigurationId>();

    [Fact]
    public async Task when_a_configuration_exists_then_it_is_returned()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration();
        repository.First = (Option<ScrapeConfigurationEntity>)configuration;

        var loaded = await unitOfWork.LoadScrapeConfigurationAsync(TestContext.Current.CancellationToken);

        loaded.ShouldBeSameAs(configuration);
    }

    [Fact]
    public async Task when_the_token_is_cancelled_then_the_load_is_cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => unitOfWork.LoadScrapeConfigurationAsync(cancellation.Token));
    }

    [Fact]
    public async Task when_no_configuration_exists_then_it_throws_an_invalid_operation_exception()
    {
        repository.First = Option<ScrapeConfigurationEntity>.None.Instance;

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => unitOfWork.LoadScrapeConfigurationAsync(TestContext.Current.CancellationToken));

        thrown.Message.ShouldBe("Scrape configuration not found");
    }

    [Fact]
    public async Task when_the_lookup_fails_then_the_failure_is_rethrown()
    {
        var failure = new InvalidOperationException("query failed");
        repository.First = failure;

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => unitOfWork.LoadScrapeConfigurationAsync(TestContext.Current.CancellationToken));

        thrown.ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task when_the_lookup_fails_then_the_original_throw_site_is_kept_in_the_stack_trace()
    {
        repository.First = ThrownFailure.Create("query failed");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => unitOfWork.LoadScrapeConfigurationAsync(TestContext.Current.CancellationToken));

        ThrownFailure.TraceOf(thrown).ShouldContain(ThrownFailure.ThrowSite);
    }
}
