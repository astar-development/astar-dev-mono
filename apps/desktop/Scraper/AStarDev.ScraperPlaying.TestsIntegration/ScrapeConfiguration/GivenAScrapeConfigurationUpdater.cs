using AStarDev.ScraperPlaying.Scoping;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.ScrapeConfiguration;

/// <summary>Exercises <see cref="ScrapeConfigurationUpdater"/> against the real data services and a temp SQLite file.</summary>
public sealed class GivenAScrapeConfigurationUpdater : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-updater-{Guid.CreateVersion7():N}.db");
    private readonly ServiceProvider serviceProvider;
    private readonly ScrapeConfigurationUpdater updater;
    private bool disposed;

    public GivenAScrapeConfigurationUpdater()
    {
        serviceProvider = new ServiceCollection().AddDataServices(databasePath).BuildServiceProvider();
        updater = new ScrapeConfigurationUpdater(new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()), TimeProvider.System);
    }

    [Fact]
    public async Task when_root_settings_are_saved_then_a_fresh_read_returns_them_and_the_children_are_unchanged()
    {
        var id = await SeedAsync();
        var current = RootSettings.From(await ReadAsync(id));
        var settings = current with
        {
            Urls = current.Urls with { BaseUrl = new Uri("https://other.example/api") },
            ApiKey = "new-key",
            Pages = current.Pages with { Search = current.Pages.Search with { Total = 99 } },
            Browser = new BrowserOptions(true, Option.Some(125f))
        };

        var result = await updater.SaveAsync(id, [settings], TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeTrue();
        var reloaded = await ReadAsync(id);
        reloaded.BaseUrl.ShouldBe(new Uri("https://other.example/api"));
        reloaded.ApiKey.ShouldBe("new-key");
        reloaded.TotalPages.ShouldBe(99);
        reloaded.UseHeadless.ShouldBeTrue();
        reloaded.SlowMotionDelay.ShouldBe(125f);
        reloaded.SearchConfiguration.SearchTerm.ShouldBe("search-config");
        reloaded.UserConfiguration.Username.ShouldBe("username");
        reloaded.ScrapeDirectories.RootDirectory.ShouldBe("root-save-directory");
    }

    [Fact]
    public async Task when_user_settings_are_saved_then_a_fresh_read_returns_them_and_the_root_settings_are_unchanged()
    {
        var id = await SeedAsync();

        var result = await updater.SaveAsync(id, [new UserSettings("new@example.test", "new-user", "new-key")], TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeTrue();
        var reloaded = await ReadAsync(id);
        reloaded.UserConfiguration.EmailAddress.ShouldBe("new@example.test");
        reloaded.UserConfiguration.Username.ShouldBe("new-user");
        reloaded.UserConfiguration.ApiKey.ShouldBe("new-key");
        reloaded.BaseUrl.ShouldBe(new Uri("https://example.com/scrape"));
        reloaded.SearchConfiguration.SearchTerm.ShouldBe("search-config");
    }

    [Fact]
    public async Task when_directory_settings_are_saved_then_a_fresh_read_returns_them_and_the_other_sections_are_unchanged()
    {
        var id = await SeedAsync();

        var result = await updater.SaveAsync(id, [new DirectorySettings("/new/root", "/new/famous", "new-sub")], TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeTrue();
        var reloaded = await ReadAsync(id);
        reloaded.ScrapeDirectories.RootDirectory.ShouldBe("/new/root");
        reloaded.ScrapeDirectories.RootDirectoryFamous.ShouldBe("/new/famous");
        reloaded.ScrapeDirectories.SubDirectoryName.ShouldBe("new-sub");
        reloaded.UserConfiguration.Username.ShouldBe("username");
        reloaded.BaseUrl.ShouldBe(new Uri("https://example.com/scrape"));
    }

    [Fact]
    public async Task when_search_settings_are_saved_then_a_fresh_read_returns_them_and_the_other_sections_are_unchanged()
    {
        var id = await SeedAsync();

        var result = await updater.SaveAsync(id, [new SearchSettings("dogs", Option.Some(50))], TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeTrue();
        var reloaded = await ReadAsync(id);
        reloaded.SearchConfiguration.SearchTerm.ShouldBe("dogs");
        reloaded.SearchConfiguration.MaxResults.ShouldBe(50);
        reloaded.UserConfiguration.Username.ShouldBe("username");
        reloaded.ScrapeDirectories.RootDirectory.ShouldBe("root-save-directory");
    }

    [Fact]
    public async Task when_max_results_is_cleared_then_it_is_stored_as_null()
    {
        var id = await SeedAsync();

        await updater.SaveAsync(id, [new SearchSettings("search-config", Option.None<int>())], TestContext.Current.CancellationToken);

        (await ReadAsync(id)).SearchConfiguration.MaxResults.ShouldBeNull();
    }

    [Fact]
    public async Task when_search_categories_are_saved_then_edits_additions_and_removals_persist_and_progress_is_preserved()
    {
        var id = await SeedAsync(
            new SearchCategoryEntity { Id = "cat-keep", Name = "Keep", LastKnownImageCount = 7, LastPageVisited = 3, TotalPages = 9 },
            new SearchCategoryEntity { Id = "cat-remove", Name = "Remove" });

        var result = await updater.SaveAsync(
            id,
            [new SearchCategoriesSettings([
                new SearchCategorySettings("cat-keep", "Renamed", false, true, true),
                new SearchCategorySettings("cat-new", "Added", true, false, false)
            ])],
            TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeTrue();
        var categories = (await ReadAsync(id)).SearchConfiguration.SearchCategories.OrderBy(category => category.Id).ToList();
        categories.Select(category => category.Id).ShouldBe(["cat-keep", "cat-new"]);
        var kept = categories[0];
        kept.Name.ShouldBe("Renamed");
        kept.IncludeInSearch.ShouldBeFalse();
        kept.IsFamous.ShouldBeTrue();
        kept.IsInternet.ShouldBeTrue();
        (kept.LastKnownImageCount, kept.LastPageVisited, kept.TotalPages).ShouldBe((7, 3, 9));
        var added = categories[1];
        added.Name.ShouldBe("Added");
        (added.LastKnownImageCount, added.LastPageVisited, added.TotalPages).ShouldBe((0, 0, 0));
    }

    [Fact]
    public async Task when_every_search_category_is_removed_then_none_remain_and_the_search_term_is_unchanged()
    {
        var id = await SeedAsync(new SearchCategoryEntity { Id = "cat-only", Name = "Only" });

        await updater.SaveAsync(id, [new SearchCategoriesSettings([])], TestContext.Current.CancellationToken);

        var reloaded = await ReadAsync(id);
        reloaded.SearchConfiguration.SearchCategories.ShouldBeEmpty();
        reloaded.SearchConfiguration.SearchTerm.ShouldBe("search-config");
    }

    [Fact]
    public async Task when_a_new_search_category_reuses_an_id_from_another_configuration_then_the_save_fails_and_nothing_changes()
    {
        var id = await SeedAsync(new SearchCategoryEntity { Id = "cat-keep", Name = "Keep" });
        var otherId = await SeedAsync(new SearchCategoryEntity { Id = "cat-other", Name = "Other" });

        var result = await updater.SaveAsync(
            otherId,
            [new SearchCategoriesSettings([new SearchCategorySettings("CAT-KEEP", "Clash", true, false, false), new SearchCategorySettings("cat-other", "Other", true, false, false)])],
            TestContext.Current.CancellationToken);

        result.Match(_ => false, _ => true).ShouldBeTrue();
        (await ReadAsync(id)).SearchConfiguration.SearchCategories.Single().Name.ShouldBe("Keep");
        (await ReadAsync(otherId)).SearchConfiguration.SearchCategories.Single().Name.ShouldBe("Other");
    }

    [Fact]
    public async Task when_person_categories_are_saved_then_renames_additions_and_removals_persist()
    {
        var id = await SeedWithPersonCategoriesAsync(["Celebrities", "Models", "Actress"]);
        var existing = (await ReadAsync(id)).SearchConfiguration.PersonCategories.ToDictionary(category => category.Name, category => category.Id);

        var result = await updater.SaveAsync(
            id,
            [new PersonCategoriesSettings([
                new PersonCategorySettings(Option.Some(existing["Celebrities"]), "Famous People"),
                new PersonCategorySettings(Option.Some(existing["Models"]), "Models"),
                new PersonCategorySettings(Option.None<Guid>(), "Athletes")
            ])],
            TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeTrue();
        var categories = (await ReadAsync(id)).SearchConfiguration.PersonCategories.ToList();
        categories.Select(category => category.Name).Order().ShouldBe(["Athletes", "Famous People", "Models"]);
        categories.Single(category => category.Name == "Famous People").Id.ShouldBe(existing["Celebrities"]);
        categories.Single(category => category.Name == "Models").Id.ShouldBe(existing["Models"]);
    }

    [Fact]
    public async Task when_every_person_category_is_removed_then_none_remain_and_the_search_categories_are_unchanged()
    {
        var id = await SeedWithPersonCategoriesAsync(["Celebrities"], new SearchCategoryEntity { Id = "cat-only", Name = "Only" });

        await updater.SaveAsync(id, [new PersonCategoriesSettings([])], TestContext.Current.CancellationToken);

        var reloaded = await ReadAsync(id);
        reloaded.SearchConfiguration.PersonCategories.ShouldBeEmpty();
        reloaded.SearchConfiguration.SearchCategories.Single().Name.ShouldBe("Only");
    }

    [Fact]
    public async Task when_the_slow_motion_delay_is_cleared_then_it_is_stored_as_null()
    {
        var id = await SeedAsync();

        var current = RootSettings.From(await ReadAsync(id));

        await updater.SaveAsync(id, [current with { Browser = current.Browser with { SlowMotionDelay = Option.None<float>() } }], TestContext.Current.CancellationToken);

        (await ReadAsync(id)).SlowMotionDelay.ShouldBeNull();
    }

    [Fact]
    public async Task when_the_configuration_does_not_exist_then_none_is_returned_and_nothing_is_created()
    {
        var seededId = await SeedAsync();
        var settings = RootSettings.From(await ReadAsync(seededId));
        var countBefore = (await ListIdsAsync()).Count;

        var result = await updater.SaveAsync(new ScrapeConfigurationId(Guid.CreateVersion7()), [settings], TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => true, () => false).ShouldBeFalse();
        (await ListIdsAsync()).Count.ShouldBe(countBefore);
    }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        serviceProvider.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }

    private Task<ScrapeConfigurationId> SeedAsync(params SearchCategoryEntity[] categories) => SeedWithPersonCategoriesAsync([], categories);

    private async Task<ScrapeConfigurationId> SeedWithPersonCategoriesAsync(string[] personCategoryNames, params SearchCategoryEntity[] categories)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ControlDbContext>();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var rootId = new ScrapeConfigurationId(Guid.Empty);
        var entity = new ScrapeConfigurationEntity(rootId)
        {
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.Empty), rootId, "user@example.com", "username", "apiKey"),
            SearchConfiguration = new SearchConfigurationEntity(new SearchConfigurationId(Guid.Empty), rootId, "search-config", 10, [.. categories]),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.Empty), rootId, "root-save-directory", "root-directory-famous", "sub-directory-name"),
            BaseUrl = new Uri("https://example.com/scrape"),
            LoginUrl = "https://example.com/scrape/login",
            SlowMotionDelay = 250f
        };
        foreach (var name in personCategoryNames) entity.SearchConfiguration.PersonCategories.Add(new PersonCategoryEntity { Name = name });
        var added = context.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().Add(entity).Match(value => value, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return added.Id;
    }

    private async Task<ScrapeConfigurationEntity> ReadAsync(ScrapeConfigurationId id)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

        return (await repository.TryFindAsync(id)).Match(option => option, exception => throw exception).Match(entity => entity, () => throw new InvalidOperationException("Not found."));
    }

    private async Task<IReadOnlyList<ScrapeConfigurationId>> ListIdsAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ControlDbContext>();

        return await context.ScrapeConfigurations.AsNoTracking().Select(entity => entity.Id).ToListAsync();
    }
}
