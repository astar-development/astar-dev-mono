using AStarDev.WallpaperScraper.Scoping;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using Microsoft.Extensions.DependencyInjection;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.TestsUnit.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationImporter : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private int unitOfWorkResolutions;
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository;
    private readonly ScrapeConfigurationImporter importer;
    private readonly FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero));

    public GivenAScrapeConfigurationImporter()
    {
        repository = unitOfWork.Register<ScrapeConfigurationEntity, ScrapeConfigurationId>();
        serviceProvider = new ServiceCollection().AddScoped<IUnitOfWork>(_ =>
        {
            unitOfWorkResolutions++;

            return unitOfWork;
        }).BuildServiceProvider();
        importer = new ScrapeConfigurationImporter(new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()), timeProvider);
    }

    public void Dispose() => serviceProvider.Dispose();

    [Fact]
    public async Task when_a_document_is_imported_then_the_stored_person_categories_are_stamped_with_the_time_provider_time()
    {
        _ = await importer.ImportScrapeConfigurationAsync(CreateDocument("user"), TestContext.Current.CancellationToken);

        var search = repository.Added.Single().SearchConfiguration;
        search.PersonCategories.Select(category => category.CreatedAt).Distinct().ShouldBe([timeProvider.GetUtcNow()]);
    }

    [Fact]
    public async Task when_several_imports_run_then_each_resolves_its_own_unit_of_work()
    {
        _ = await importer.ImportScrapeConfigurationAsync(CreateDocument("first"), TestContext.Current.CancellationToken);
        _ = await importer.ImportScrapeConfigurationAsync(CreateDocument("second"), TestContext.Current.CancellationToken);

        unitOfWorkResolutions.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_token_is_cancelled_then_the_import_is_cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => importer.ImportScrapeConfigurationAsync(CreateDocument("user"), cancellation.Token));
    }

    [Fact]
    public async Task when_a_document_is_imported_then_the_delete_and_add_happen_inside_a_single_transaction()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)StoredConfigurationWithKeys("stored-scrape-key", "stored-user-key");

        _ = await importer.ImportScrapeConfigurationAsync(CreateDocument("user"), TestContext.Current.CancellationToken);

        unitOfWork.TransactionCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_document_cannot_be_mapped_then_the_existing_configuration_is_not_deleted_and_nothing_is_saved()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)StoredConfigurationWithKeys("stored-scrape-key", "stored-user-key");
        var unmappable = new ScrapeConfigurationImportDocument { SearchConfiguration = new SearchConfigurationImportDocument { SearchCategories = [null!] } };

        var result = await importer.ImportScrapeConfigurationAsync(unmappable, TestContext.Current.CancellationToken);

        (result.Match(_ => true, _ => false), repository.Deleted.Count, unitOfWork.SaveCount).ShouldBe((false, 0, 0));
    }

    [Fact]
    public async Task when_the_file_has_no_api_keys_then_the_stored_api_keys_are_kept()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)StoredConfigurationWithKeys("stored-scrape-key", "stored-user-key");

        _ = await importer.ImportScrapeConfigurationAsync(CreateDocument("user"), TestContext.Current.CancellationToken);

        (repository.Added.Single().ApiKey, repository.Added.Single().UserConfiguration.ApiKey).ShouldBe(("stored-scrape-key", "stored-user-key"));
    }

    [Fact]
    public async Task when_the_file_has_api_keys_then_they_replace_the_stored_ones()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)StoredConfigurationWithKeys("stored-scrape-key", "stored-user-key");
        var document = new ScrapeConfigurationImportDocument { ApiKey = "file-scrape-key", UserConfiguration = new() { ApiKey = "file-user-key" }, SearchConfiguration = new(), ScrapeDirectories = new() };

        _ = await importer.ImportScrapeConfigurationAsync(document, TestContext.Current.CancellationToken);

        (repository.Added.Single().ApiKey, repository.Added.Single().UserConfiguration.ApiKey).ShouldBe(("file-scrape-key", "file-user-key"));
    }

    [Fact]
    public async Task when_the_file_has_no_api_keys_and_nothing_is_stored_then_the_keys_stay_empty()
    {
        repository.First = Option<ScrapeConfigurationEntity>.None.Instance;

        _ = await importer.ImportScrapeConfigurationAsync(CreateDocument("user"), TestContext.Current.CancellationToken);

        (repository.Added.Single().ApiKey, repository.Added.Single().UserConfiguration.ApiKey).ShouldBe((string.Empty, string.Empty));
    }

    [Fact]
    public async Task when_only_one_key_is_missing_from_the_file_then_only_that_one_is_kept_from_the_stored_configuration()
    {
        repository.First = (Option<ScrapeConfigurationEntity>)StoredConfigurationWithKeys("stored-scrape-key", "stored-user-key");
        var document = new ScrapeConfigurationImportDocument { ApiKey = "file-scrape-key", UserConfiguration = new(), SearchConfiguration = new(), ScrapeDirectories = new() };

        _ = await importer.ImportScrapeConfigurationAsync(document, TestContext.Current.CancellationToken);

        (repository.Added.Single().ApiKey, repository.Added.Single().UserConfiguration.ApiKey).ShouldBe(("file-scrape-key", "stored-user-key"));
    }

    private static ScrapeConfigurationEntity StoredConfigurationWithKeys(string scrapeKey, string userKey)
    {
        var id = new ScrapeConfigurationId(Guid.CreateVersion7());

        return new ScrapeConfigurationEntity(id)
        {
            ApiKey = scrapeKey,
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), id, "stored@example.test", "stored", userKey)
        };
    }

    [Fact]
    public async Task when_no_configuration_exists_then_the_document_is_added_without_a_prior_delete()
    {
        repository.First = Option<ScrapeConfigurationEntity>.None.Instance;

        var result = await importer.ImportScrapeConfigurationAsync(CreateDocument("user"), TestContext.Current.CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        repository.Deleted.ShouldBeEmpty();
        repository.Added.Single().UserConfiguration.Username.ShouldBe("user");
        unitOfWork.SaveCount.ShouldBe(1);
        unitOfWork.Operations.ShouldBe(["add", "save"]);
    }

    [Fact]
    public async Task when_a_configuration_already_exists_then_it_is_deleted_before_the_new_one_is_added()
    {
        var existing = StoredConfigurationWithKeys("stored-scrape-key", "stored-user-key");
        repository.First = (Option<ScrapeConfigurationEntity>)existing;

        var result = await importer.ImportScrapeConfigurationAsync(CreateDocument("new-user"), TestContext.Current.CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        repository.Deleted.ShouldBe([existing]);
        repository.Added.Single().UserConfiguration.Username.ShouldBe("new-user");
        unitOfWork.SaveCount.ShouldBe(2);
        unitOfWork.Operations.ShouldBe(["delete", "save", "add", "save"]);
    }

    [Fact]
    public async Task when_looking_up_the_existing_configuration_fails_then_the_failure_is_returned_and_nothing_is_written()
    {
        var exception = new InvalidOperationException("lookup failed");
        repository.First = exception;

        var result = await importer.ImportScrapeConfigurationAsync(new ScrapeConfigurationImportDocument(), TestContext.Current.CancellationToken);

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeSameAs(exception);
        unitOfWork.Operations.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_adding_the_new_configuration_fails_then_the_failure_is_returned_and_nothing_is_saved()
    {
        var exception = new InvalidOperationException("add failed");
        repository.First = Option<ScrapeConfigurationEntity>.None.Instance;
        repository.AddFailure = Option.Some<Exception>(exception);

        var result = await importer.ImportScrapeConfigurationAsync(new ScrapeConfigurationImportDocument(), TestContext.Current.CancellationToken);

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeSameAs(exception);
        unitOfWork.SaveCount.ShouldBe(0);
    }

    private static ScrapeConfigurationImportDocument CreateDocument(string username) => new()
    {
        UserConfiguration = new() { Username = username },
        SearchConfiguration = new() { SearchTerm = "cats", SearchCategories = [] },
        ScrapeDirectories = new() { RootDirectory = "/tmp" }
    };
}
