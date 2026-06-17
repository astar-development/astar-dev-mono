using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenACategoryResolutionService
{
    private static (AppDbContext seedingContext, IDbContextFactory<AppDbContext> factory) CreateInMemoryFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var seedingContext = new AppDbContext(options);
        _ = seedingContext.Database.EnsureCreated();
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));

        return (seedingContext, factory);
    }

    private static ICategoryResolutionService CreateSut(IDbContextFactory<AppDbContext> factory) => new CategoryResolutionService(factory);

    [Fact]
    public async Task when_level1_exists_then_returns_existing_id()
    {
        var (db, factory) = CreateInMemoryFactory();
        var existing = new FileClassificationCategoryEntity { Name = "Photos", Level = 1 };
        db.FileClassificationCategories.Add(existing);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var classification = FileClassificationFactory.Create("Photos", Option.None<string>(), Option.None<string>(), false);
        var sut = CreateSut(factory);

        var result = await sut.ResolveManyAsync([classification], TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        result[0].ShouldBe(existing.Id);
    }

    [Fact]
    public async Task when_level1_missing_then_creates_and_returns_new_id()
    {
        var (_, factory) = CreateInMemoryFactory();
        var classification = FileClassificationFactory.Create("Photos", Option.None<string>(), Option.None<string>(), false);
        var sut = CreateSut(factory);

        var result = await sut.ResolveManyAsync([classification], TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        result[0].ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task when_level1_and_level2_then_resolves_both_and_returns_level2_id()
    {
        var (db, factory) = CreateInMemoryFactory();
        var level1 = new FileClassificationCategoryEntity { Name = "Photos", Level = 1 };
        db.FileClassificationCategories.Add(level1);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var level2 = new FileClassificationCategoryEntity { Name = "Holidays", Level = 2, ParentId = level1.Id };
        db.FileClassificationCategories.Add(level2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var classification = FileClassificationFactory.Create("Photos", Option.Some("Holidays"), Option.None<string>(), false);
        var sut = CreateSut(factory);

        var result = await sut.ResolveManyAsync([classification], TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        result[0].ShouldBe(level2.Id);
    }

    [Fact]
    public async Task when_level1_level2_level3_then_returns_leaf_id()
    {
        var (db, factory) = CreateInMemoryFactory();
        var level1 = new FileClassificationCategoryEntity { Name = "Photos", Level = 1 };
        db.FileClassificationCategories.Add(level1);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var level2 = new FileClassificationCategoryEntity { Name = "Holidays", Level = 2, ParentId = level1.Id };
        db.FileClassificationCategories.Add(level2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var level3 = new FileClassificationCategoryEntity { Name = "Christmas", Level = 3, ParentId = level2.Id };
        db.FileClassificationCategories.Add(level3);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var classification = FileClassificationFactory.Create("Photos", Option.Some("Holidays"), Option.Some("Christmas"), false);
        var sut = CreateSut(factory);

        var result = await sut.ResolveManyAsync([classification], TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        result[0].ShouldBe(level3.Id);
    }

    [Fact]
    public async Task when_level1_exists_but_level2_missing_then_creates_level2_only()
    {
        var (db, factory) = CreateInMemoryFactory();
        var level1 = new FileClassificationCategoryEntity { Name = "Photos", Level = 1 };
        db.FileClassificationCategories.Add(level1);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var classification = FileClassificationFactory.Create("Photos", Option.Some("Holidays"), Option.None<string>(), false);
        var sut = CreateSut(factory);

        await sut.ResolveManyAsync([classification], TestContext.Current.CancellationToken);

        var allCategories = db.FileClassificationCategories.ToList();
        allCategories.Count(c => c.Level == 1).ShouldBe(1);
        allCategories.Count(c => c.Level == 2).ShouldBe(1);
        allCategories.Single(c => c.Level == 2).ParentId.ShouldBe(level1.Id);
    }

    [Fact]
    public async Task when_duplicate_classifications_then_returns_distinct_ids()
    {
        var (_, factory) = CreateInMemoryFactory();
        var classification = FileClassificationFactory.Create("Photos", Option.None<string>(), Option.None<string>(), false);
        var sut = CreateSut(factory);

        var result = await sut.ResolveManyAsync([classification, classification], TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
    }
}
