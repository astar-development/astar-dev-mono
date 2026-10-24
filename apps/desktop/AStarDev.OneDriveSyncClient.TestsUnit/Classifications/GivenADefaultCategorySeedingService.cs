using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStarDev.OneDriveSyncClient.Classifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Classifications;

public sealed class GivenADefaultCategorySeedingService : IDisposable
{
    private static readonly string[] DefaultRoots = ["Unclassified", "Colour", "Person", "Place", "Event", "Photo", "Video", "Audio", "Document", "Code", "Archive", "Other"];

    private readonly SqliteConnection connection;
    private readonly AppDbContext seedingContext;
    private readonly DefaultCategorySeedingService sut;
    private bool disposed;

    public GivenADefaultCategorySeedingService()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        seedingContext = new AppDbContext(options);
        _ = seedingContext.Database.EnsureCreated();
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
        sut = new DefaultCategorySeedingService(factory);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (!disposing)
            return;

        seedingContext.Dispose();
        connection.Dispose();
    }

    private List<FileClassificationCategoryEntity> AllCategories() => [.. seedingContext.FileClassificationCategories.AsNoTracking()];

    [Fact]
    public async Task when_seeding_then_every_default_root_exists_exactly_once()
    {
        _ = await sut.SeedDefaultCategoriesAsync(TestContext.Current.CancellationToken);

        var roots = AllCategories().Where(category => category.Level == 1).Select(category => category.Name).ToList();

        DefaultRoots.ShouldAllBe(name => roots.Count(root => string.Equals(root, name, StringComparison.OrdinalIgnoreCase)) == 1);
    }

    [Fact]
    public async Task when_seeding_then_level_two_defaults_are_created_under_their_parents()
    {
        _ = await sut.SeedDefaultCategoriesAsync(TestContext.Current.CancellationToken);

        var categories = AllCategories();
        int documentId = categories.Single(category => category.Level == 1 && category.Name == "Document").Id;
        int colourId = categories.Single(category => category.Level == 1 && category.Name == "Colour").Id;

        categories.ShouldContain(category => category.Level == 2 && category.Name == "Spreadsheet" && category.ParentId == documentId);
        categories.ShouldContain(category => category.Level == 2 && category.Name == "Red" && category.ParentId == colourId);
    }

    [Fact]
    public async Task when_seeding_twice_then_the_second_run_adds_nothing()
    {
        _ = await sut.SeedDefaultCategoriesAsync(TestContext.Current.CancellationToken);
        int countAfterFirstRun = AllCategories().Count;

        var second = await sut.SeedDefaultCategoriesAsync(TestContext.Current.CancellationToken);

        (second.Match(added => added, _ => -1), AllCategories().Count).ShouldBe((0, countAfterFirstRun));
    }

    [Fact]
    public async Task when_seeding_then_the_number_of_categories_added_is_returned()
    {
        int before = AllCategories().Count;

        var result = await sut.SeedDefaultCategoriesAsync(TestContext.Current.CancellationToken);

        result.Match(added => added, _ => -1).ShouldBe(AllCategories().Count - before);
    }

    [Fact]
    public async Task when_a_root_exists_with_different_casing_then_it_is_not_duplicated_or_altered()
    {
        seedingContext.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "person", Level = 1, IncludeInSearch = false });
        await seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _ = await sut.SeedDefaultCategoriesAsync(TestContext.Current.CancellationToken);

        var people = AllCategories().Where(category => category.Level == 1 && string.Equals(category.Name, "Person", StringComparison.OrdinalIgnoreCase)).ToList();
        (people.Count, people[0].Name, people[0].IncludeInSearch).ShouldBe((1, "person", false));
    }

    [Fact]
    public async Task when_the_same_child_name_exists_under_another_parent_then_it_does_not_block_the_default()
    {
        var person = new FileClassificationCategoryEntity { Name = "Person", Level = 1 };
        seedingContext.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Red", Level = 2, Parent = person });
        await seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _ = await sut.SeedDefaultCategoriesAsync(TestContext.Current.CancellationToken);

        var categories = AllCategories();
        int colourId = categories.Single(category => category.Level == 1 && category.Name == "Colour").Id;
        categories.ShouldContain(category => category.Level == 2 && category.Name == "Red" && category.ParentId == colourId);
    }

    [Theory]
    [InlineData("Code")]
    [InlineData("Archive")]
    [InlineData("Other")]
    public async Task when_seeding_then_the_noisy_roots_are_excluded_from_search(string name)
    {
        _ = await sut.SeedDefaultCategoriesAsync(TestContext.Current.CancellationToken);

        AllCategories().Single(category => category.Level == 1 && category.Name == name).IncludeInSearch.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Photo")]
    [InlineData("Video")]
    [InlineData("Audio")]
    [InlineData("Document")]
    [InlineData("Place")]
    [InlineData("Event")]
    public async Task when_seeding_then_the_content_roots_are_included_in_search(string name)
    {
        _ = await sut.SeedDefaultCategoriesAsync(TestContext.Current.CancellationToken);

        AllCategories().Single(category => category.Level == 1 && category.Name == name).IncludeInSearch.ShouldBeTrue();
    }

    [Fact]
    public async Task when_seeding_then_level_two_defaults_inherit_the_search_setting_of_their_parent()
    {
        _ = await sut.SeedDefaultCategoriesAsync(TestContext.Current.CancellationToken);

        var categories = AllCategories();
        int codeId = categories.Single(category => category.Level == 1 && category.Name == "Code").Id;

        categories.Where(category => category.ParentId == codeId).ShouldAllBe(category => !category.IncludeInSearch);
    }
}
