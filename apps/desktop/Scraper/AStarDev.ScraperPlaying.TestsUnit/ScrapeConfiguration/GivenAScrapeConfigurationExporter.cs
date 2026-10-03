using AStarDev.ScraperPlaying.Scoping;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationExporter : IDisposable
{
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly ServiceProvider serviceProvider;
    private int unitOfWorkResolutions;
    private readonly FakeRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository;

    public GivenAScrapeConfigurationExporter()
    {
        repository = unitOfWork.Register<ScrapeConfigurationEntity, ScrapeConfigurationId>();
        serviceProvider = new ServiceCollection().AddScoped<IUnitOfWork>(_ =>
        {
            unitOfWorkResolutions++;

            return unitOfWork;
        }).BuildServiceProvider();
    }

    public void Dispose() => serviceProvider.Dispose();

    [Fact]
    public async Task when_several_exports_run_then_each_resolves_its_own_unit_of_work()
    {
        var exporter = CreateExporter();

        _ = await exporter.ExportScrapeConfigurationAsync(ApiKeyExport.Include, TestContext.Current.CancellationToken);
        _ = await exporter.ExportScrapeConfigurationAsync(ApiKeyExport.Include, TestContext.Current.CancellationToken);

        unitOfWorkResolutions.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_token_is_cancelled_then_the_export_is_cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var exporter = CreateExporter();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => exporter.ExportScrapeConfigurationAsync(ApiKeyExport.Include, cancellation.Token));
    }

    private ScrapeConfigurationExporter CreateExporter() => new(new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()));

    [Fact]
    public async Task when_a_configuration_exists_then_it_is_returned_as_a_document()
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());
        var searchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7());
        var existing = new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, "user@example.test", "user", "api-key"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "cats", 10, [
                new SearchCategoryEntity { SearchConfigurationId = searchConfigurationId, Id = "1", Name = "General" }
            ]),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, "Pictures", "Pictures/Famous", "Wallhaven")
        };
        repository.First = (Option<ScrapeConfigurationEntity>)existing;
        var exporter = CreateExporter();

        var result = await exporter.ExportScrapeConfigurationAsync(ApiKeyExport.Include, TestContext.Current.CancellationToken);

        var document = result.Match(option => option, exception => throw exception).Match(value => (ScrapeConfigurationImportDocument?)value, () => null);
        document.ShouldNotBeNull();
        document.UserConfiguration.Username.ShouldBe("user");
        document.SearchConfiguration.SearchTerm.ShouldBe("cats");
        document.SearchConfiguration.SearchCategories.Single().Name.ShouldBe("General");
        document.ScrapeDirectories.RootDirectory.ShouldBe("Pictures");
    }

    [Fact]
    public async Task when_no_configuration_exists_then_none_is_returned()
    {
        repository.First = Option<ScrapeConfigurationEntity>.None.Instance;
        var exporter = CreateExporter();

        var result = await exporter.ExportScrapeConfigurationAsync(ApiKeyExport.Include, TestContext.Current.CancellationToken);

        var hasDocument = result.Match(option => option, exception => throw exception).Match(_ => true, () => false);
        hasDocument.ShouldBeFalse();
    }

    [Fact]
    public async Task when_looking_up_the_configuration_fails_then_the_failure_is_returned()
    {
        var exception = new InvalidOperationException("lookup failed");
        repository.First = exception;
        var exporter = CreateExporter();

        var result = await exporter.ExportScrapeConfigurationAsync(ApiKeyExport.Include, TestContext.Current.CancellationToken);

        var capturedException = result.Match(_ => (Exception?)null, ex => ex);
        capturedException.ShouldBeSameAs(exception);
    }
}
