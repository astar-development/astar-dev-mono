using AStarDev.WallpaperScraper.Scoping;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testably.Abstractions;
using Xunit;

namespace AStarDev.WallpaperScraper.TestsIntegration.ScrapeConfiguration;

/// <summary>
/// Edits every section of a scrape configuration through <see cref="ScrapeConfigurationUpdater"/> (the editor's save path),
/// exports it to a file, imports that file into a second, clean database and checks every edited value survived.
/// </summary>
public sealed class GivenAConfigurationRoundTrip : IDisposable
{
    private readonly string exportPath = Path.Combine(Path.GetTempPath(), $"astardev-WallpaperScraper-roundtrip-{Guid.CreateVersion7():N}.json");
    private readonly TestDatabase source = new("source");
    private readonly TestDatabase target = new("target");
    private bool disposed;

    [Fact]
    public async Task when_every_section_is_edited_then_export_and_import_into_a_clean_database_preserve_every_value()
    {
        var id = await source.CreateAsync();
        await source.SetProgressAsync("1", lastKnownImageCount: 7, lastPageVisited: 3, totalPages: 9);
        var personIds = (await source.ReadAsync(id)).SearchConfiguration.PersonCategories.ToDictionary(category => category.Name, category => category.Id);
        var edits = new IScrapeConfigurationSectionEdit[]
        {
            new RootSettings(
                new WallhavenUrls(new Uri("https://edited.example/api/v1"), "edited login: not a url, /login?x=1 & more", "edited-top", "edited-hot", "edited-subscriptions"),
                "edited-api-key",
                "edited search",
                "edited-prefix",
                "edited-suffix",
                11,
                new PageRanges(new PageRange(2, 22), new PageRange(3, 33), new PageRange(4, 44), new PageRange(5, 55)),
                new BrowserOptions(true, Option.Some(125f))),
            new UserSettings("edited@example.test", "edited-user", "edited-user-key"),
            new DirectorySettings("/edited/root", "/edited/famous", "edited-sub"),
            new SearchSettings("edited term", Option.Some(77)),
            new SearchCategoriesSettings([
                new SearchCategorySettings("1", "Renamed category", false, false, true),
                new SearchCategorySettings("3", "category3", true, false, false),
                new SearchCategorySettings("edited-new", "Added category", true, true, false)
            ]),
            new PersonCategoriesSettings([
                new PersonCategorySettings(Option.Some(personIds["Celebrities"]), "Famous People"),
                new PersonCategorySettings(Option.Some(personIds["Models"]), "Models"),
                new PersonCategorySettings(Option.None<Guid>(), "Athletes")
            ])
        };
        (await source.Updater.SaveAsync(id, edits, TestContext.Current.CancellationToken)).Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeTrue();

        (await source.ExportAsync(exportPath)).ShouldBeTrue();
        await target.ImportAsync(exportPath);

        var imported = await target.ReadFirstAsync();
        imported.Id.ShouldBe(id);
        imported.BaseUrl.ShouldBe(new Uri("https://edited.example/api/v1"));
        imported.LoginUrl.ShouldBe("edited login: not a url, /login?x=1 & more");
        imported.ApiKey.ShouldBe("edited-api-key");
        imported.SearchString.ShouldBe("edited search");
        imported.TopWallpapers.ShouldBe("edited-top");
        imported.HotWallpapers.ShouldBe("edited-hot");
        imported.SearchStringPrefix.ShouldBe("edited-prefix");
        imported.SearchStringSuffix.ShouldBe("edited-suffix");
        imported.Subscriptions.ShouldBe("edited-subscriptions");
        imported.ImagePauseInSeconds.ShouldBe(11);
        (imported.StartingPageNumber, imported.TotalPages).ShouldBe((2, 22));
        (imported.SubscriptionsStartingPageNumber, imported.SubscriptionsTotalPages).ShouldBe((3, 33));
        (imported.TopWallpapersStartingPageNumber, imported.TopWallpapersTotalPages).ShouldBe((4, 44));
        (imported.HotWallpapersStartingPageNumber, imported.HotWallpapersTotalPages).ShouldBe((5, 55));
        imported.UseHeadless.ShouldBeTrue();
        imported.SlowMotionDelay.ShouldBe(125f);
        (imported.UserConfiguration.EmailAddress, imported.UserConfiguration.Username, imported.UserConfiguration.ApiKey)
            .ShouldBe(("edited@example.test", "edited-user", "edited-user-key"));
        (imported.ScrapeDirectories.RootDirectory, imported.ScrapeDirectories.RootDirectoryFamous, imported.ScrapeDirectories.SubDirectoryName)
            .ShouldBe(("/edited/root", "/edited/famous", "edited-sub"));
        imported.SearchConfiguration.SearchTerm.ShouldBe("edited term");
        imported.SearchConfiguration.MaxResults.ShouldBe(77);

        var categories = imported.SearchConfiguration.SearchCategories.OrderBy(category => category.Id).ToList();
        categories.Select(category => category.Id).ShouldBe(["1", "3", "edited-new"]);
        var renamed = categories[0];
        (renamed.Name, renamed.IncludeInSearch, renamed.IsFamous, renamed.IsInternet).ShouldBe(("Renamed category", false, false, true));
        (renamed.LastKnownImageCount, renamed.LastPageVisited, renamed.TotalPages).ShouldBe((7, 3, 9));
        var added = categories[2];
        (added.Name, added.IncludeInSearch, added.IsFamous, added.IsInternet).ShouldBe(("Added category", true, true, false));

        imported.SearchConfiguration.PersonCategories.Select(category => category.Name).Order().ShouldBe(["Athletes", "Famous People", "Models"]);
    }

    [Fact]
    public async Task when_the_default_export_is_imported_over_a_configuration_with_api_keys_then_the_file_has_no_keys_and_the_stored_keys_survive()
    {
        var id = await source.CreateAsync();
        await source.Updater.SaveAsync(id, [new RootSettings(new WallhavenUrls(new Uri("https://example.test/"), "login", "top", "hot", "subs"), "scrape-key-123", "term", "prefix", "suffix", 1, new PageRanges(new PageRange(1, 2), new PageRange(1, 2), new PageRange(1, 2), new PageRange(1, 2)), new BrowserOptions(false, Option.None<float>())), new UserSettings("user@example.test", "user", "user-key-456")], TestContext.Current.CancellationToken);

        (await source.ExportAsync(exportPath, ApiKeyExport.Exclude)).ShouldBeTrue();
        var text = await File.ReadAllTextAsync(exportPath, TestContext.Current.CancellationToken);
        await source.ImportAsync(exportPath);

        var afterImport = await source.ReadFirstAsync();
        (text.Contains("scrape-key-123", StringComparison.Ordinal), text.Contains("user-key-456", StringComparison.Ordinal), afterImport.ApiKey, afterImport.UserConfiguration.ApiKey).ShouldBe((false, false, "scrape-key-123", "user-key-456"));
    }

    [Fact]
    public async Task when_the_max_results_limit_is_cleared_then_it_is_still_unlimited_after_export_and_import()
    {
        var id = await source.CreateAsync();
        await source.Updater.SaveAsync(id, [new SearchSettings("no limit", Option.None<int>())], TestContext.Current.CancellationToken);

        await source.ExportAsync(exportPath);
        await target.ImportAsync(exportPath);

        var imported = await target.ReadFirstAsync();
        imported.SearchConfiguration.SearchTerm.ShouldBe("no limit");
        imported.SearchConfiguration.MaxResults.ShouldBeNull();
    }

    [Fact]
    public async Task when_the_exported_file_is_imported_then_the_editor_shows_the_imported_values_ready_to_save()
    {
        var id = await source.CreateAsync();
        await source.Updater.SaveAsync(id, [new SearchSettings("round trip", Option.None<int>())], TestContext.Current.CancellationToken);
        await source.ExportAsync(exportPath);
        await target.ImportAsync(exportPath);

        var imported = await target.ReadFirstAsync();

        SearchSettingsInput.From(imported).Validate().ShouldBeOfType<Valid<SearchSettings>>();
        RootSettingsInput.From(imported).Validate().ShouldBeOfType<Valid<RootSettings>>();
        UserSettingsInput.From(imported).Validate().ShouldBeOfType<Valid<UserSettings>>();
        DirectorySettingsInput.From(imported).Validate().ShouldBeOfType<Valid<DirectorySettings>>();
        SearchCategoriesInput.From(imported).Validate().ShouldBeOfType<Valid<SearchCategoriesSettings>>();
        PersonCategoriesInput.From(imported).Validate().ShouldBeOfType<Valid<PersonCategoriesSettings>>();
    }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        source.Dispose();
        target.Dispose();
        if (File.Exists(exportPath)) File.Delete(exportPath);
    }

    /// <summary>A temp SQLite database created (and seeded with the default configuration) through the real data services.</summary>
    private sealed class TestDatabase : IDisposable
    {
        private readonly string databasePath;
        private readonly ServiceProvider serviceProvider;

        public TestDatabase(string name)
        {
            databasePath = Path.Combine(Path.GetTempPath(), $"astardev-WallpaperScraper-roundtrip-{name}-{Guid.CreateVersion7():N}.db");
            serviceProvider = new ServiceCollection().AddDataServices(databasePath).BuildServiceProvider();
            Updater = new ScrapeConfigurationUpdater(new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()), TimeProvider.System);
        }

        public ScrapeConfigurationUpdater Updater { get; }

        public async Task<ScrapeConfigurationId> CreateAsync()
        {
            using var scope = serviceProvider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ControlDbContext>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

            return (await ReadFirstAsync()).Id;
        }

        public async Task SetProgressAsync(string categoryId, int lastKnownImageCount, int lastPageVisited, int totalPages)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ControlDbContext>();
            var category = await context.Set<SearchCategoryEntity>().SingleAsync(candidate => candidate.Id == categoryId, TestContext.Current.CancellationToken);
            (category.LastKnownImageCount, category.LastPageVisited, category.TotalPages) = (lastKnownImageCount, lastPageVisited, totalPages);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        public async Task<bool> ExportAsync(string path, ApiKeyExport apiKeys = ApiKeyExport.Include)
        {
            var exporter = new ScrapeConfigurationExporter(new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()));

            return await new ScrapeConfigurationExportService(exporter, new ScrapeConfigurationFileWriter(new RealFileSystem())).ExportAsync(path, apiKeys, TestContext.Current.CancellationToken);
        }

        public async Task ImportAsync(string path)
        {
            using var scope = serviceProvider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ControlDbContext>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var importer = new ScrapeConfigurationImporter(new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()), TimeProvider.System);

            await new ScrapeConfigurationImportService(importer, new ScrapeConfigurationFileReader(new RealFileSystem())).ImportAsync(path, TestContext.Current.CancellationToken);
        }

        public async Task<ScrapeConfigurationEntity> ReadFirstAsync()
        {
            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

            return (await repository.TryGetFirstAsync()).Match(option => option, exception => throw exception).Match(entity => entity, () => throw new InvalidOperationException("No configuration."));
        }

        public async Task<ScrapeConfigurationEntity> ReadAsync(ScrapeConfigurationId id)
        {
            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

            return (await repository.TryFindAsync(id)).Match(option => option, exception => throw exception).Match(entity => entity, () => throw new InvalidOperationException("Not found."));
        }

        public void Dispose()
        {
            serviceProvider.Dispose();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }
}
