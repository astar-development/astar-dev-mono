using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperCategoryRegistrar : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;

    public GivenWallpaperCategoryRegistrar()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        dbContextFactory = new TestDbContextFactory(options);

        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
    }

    [Fact]
    public async Task when_a_tagged_category_does_not_exist_then_it_is_added()
    {
        var sut = new WallpaperCategoryRegistrar(dbContextFactory);

        await sut.EnsureCategoriesExistAsync([new TagData("Nature", "outdoors")], TestContext.Current.CancellationToken);

        using var context = dbContextFactory.CreateDbContext();
        var category = context.FileClassificationCategories.Single(c => c.Name == "Nature");
        category.IsFamous.ShouldBeFalse();
        category.IsInternet.ShouldBeFalse();
        category.IncludeInSearch.ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_tagged_category_already_exists_then_it_is_not_duplicated()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Nature", });
            context.SaveChanges();
        }

        var sut = new WallpaperCategoryRegistrar(dbContextFactory);
        await sut.EnsureCategoriesExistAsync([new TagData("Nature", "outdoors")], TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        verifyContext.FileClassificationCategories.Count(c => c.Name == "Nature").ShouldBe(1);
    }

    [Fact]
    public async Task when_a_tag_has_no_category_then_it_is_not_registered()
    {
        var sut = new WallpaperCategoryRegistrar(dbContextFactory);

        await sut.EnsureCategoriesExistAsync([new TagData("Untagged", null)], TestContext.Current.CancellationToken);

        using var context = dbContextFactory.CreateDbContext();
        context.FileClassificationCategories.Any(c => c.Name == "Untagged").ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_tag_has_a_whitespace_name_then_it_is_not_registered()
    {
        var sut = new WallpaperCategoryRegistrar(dbContextFactory);

        await sut.EnsureCategoriesExistAsync([new TagData("   ", "outdoors")], TestContext.Current.CancellationToken);

        using var context = dbContextFactory.CreateDbContext();
        context.FileClassificationCategories.Any(c => c.Name == "   ").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_same_tag_appears_twice_in_one_batch_then_only_one_category_is_added()
    {
        var sut = new WallpaperCategoryRegistrar(dbContextFactory);

        await sut.EnsureCategoriesExistAsync([new TagData("Anna Friel", "people > actress"), new TagData("Anna Friel", "people > celebrity")], TestContext.Current.CancellationToken);

        using var context = dbContextFactory.CreateDbContext();
        context.FileClassificationCategories.Count(c => c.Name == "Anna Friel").ShouldBe(1);
    }

    [Fact]
    public async Task when_a_tag_differs_only_by_case_from_an_existing_category_then_it_is_not_duplicated()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Anna Friel", });
            context.SaveChanges();
        }

        var sut = new WallpaperCategoryRegistrar(dbContextFactory);
        await sut.EnsureCategoriesExistAsync([new TagData("anna friel", "people > actress")], TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        verifyContext.FileClassificationCategories.ToList().Count(c => c.Name.Equals("anna friel", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
    }

    [Fact]
    public async Task when_a_tag_differs_only_by_surrounding_whitespace_from_an_existing_category_then_it_is_not_duplicated()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Anna Friel", });
            context.SaveChanges();
        }

        var sut = new WallpaperCategoryRegistrar(dbContextFactory);
        await sut.EnsureCategoriesExistAsync([new TagData(" Anna Friel ", "people > actress")], TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        verifyContext.FileClassificationCategories.Count(c => c.Name.Contains("Anna Friel")).ShouldBe(1);
    }

    [Fact]
    public async Task when_a_new_tag_has_surrounding_whitespace_then_the_category_is_stored_trimmed()
    {
        var sut = new WallpaperCategoryRegistrar(dbContextFactory);

        await sut.EnsureCategoriesExistAsync([new TagData(" Sunset ", "outdoors")], TestContext.Current.CancellationToken);

        using var context = dbContextFactory.CreateDbContext();
        context.FileClassificationCategories.Single(c => c.Name.Contains("Sunset")).Name.ShouldBe("Sunset");
    }

    [Fact]
    public async Task when_categories_are_registered_then_the_user_managed_search_categories_are_never_written()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            var searchConfiguration = new SearchConfigurationEntity();
            searchConfiguration.SearchCategories.Add(new SearchCategoryEntity { Id = "landscapes-id", Name = "Landscapes", });
            context.SearchConfigurations.Add(searchConfiguration);
            context.SaveChanges();
        }

        var sut = new WallpaperCategoryRegistrar(dbContextFactory);
        await sut.EnsureCategoriesExistAsync([TagDataFactory.Create("Nature", "outdoors"), TagDataFactory.Create("Sunset", "outdoors")], TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        verifyContext.FileClassificationCategories.Count(c => c.Name == "Nature" || c.Name == "Sunset").ShouldBe(2);
        var searchCategory = verifyContext.SearchCategories.Single();
        searchCategory.Id.ShouldBe("landscapes-id");
        searchCategory.Name.ShouldBe("Landscapes");
    }

    public void Dispose() =>
        connection.Dispose();

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
