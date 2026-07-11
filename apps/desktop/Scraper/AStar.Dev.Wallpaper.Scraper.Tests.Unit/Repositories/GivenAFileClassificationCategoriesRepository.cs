using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Repositories;

public sealed class GivenAFileClassificationCategoriesRepository : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;
    private FileClassificationCategoriesRepository sut = null!;
    private int carsId;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var seedContext = new AppDbContext(options);
        await seedContext.Database.MigrateAsync();

        var cars = new FileClassificationCategoryEntity { Name = "Cars", Level = 1 };
        seedContext.FileClassificationCategories.Add(cars);
        await seedContext.SaveChangesAsync();

        seedContext.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Red", Level = 2, ParentId = cars.Id });
        await seedContext.SaveChangesAsync();
        carsId = cars.Id;

        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));

        sut = new FileClassificationCategoriesRepository(factory);
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_saving_a_tag_that_already_exists_under_the_same_category_then_it_is_not_duplicated()
    {
        await sut.SaveAsync([new TagData("Red", "Cars")]);

        await using var verifyContext = new AppDbContext(options);
        int count = await verifyContext.FileClassificationCategories.CountAsync(c => c.Name == "Red" && c.ParentId == carsId, TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task when_saving_a_new_tag_under_an_existing_category_then_it_is_added()
    {
        await sut.SaveAsync([new TagData("Blue", "Cars")]);

        await using var verifyContext = new AppDbContext(options);
        bool exists = await verifyContext.FileClassificationCategories.AnyAsync(c => c.Name == "Blue", TestContext.Current.CancellationToken);
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task when_upserting_a_new_category_then_it_is_persisted()
    {
        await sut.UpsertAsync([new FileClassificationCategoryEntity { Name = "Green", Level = 1, IncludeInSearch = true }], TestContext.Current.CancellationToken);

        await using var verifyContext = new AppDbContext(options);
        var saved = await verifyContext.FileClassificationCategories.SingleAsync(c => c.Name == "Green", TestContext.Current.CancellationToken);
        saved.IncludeInSearch.ShouldBeTrue();
    }
}
