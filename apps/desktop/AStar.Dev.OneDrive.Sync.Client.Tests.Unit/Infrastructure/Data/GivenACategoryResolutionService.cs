using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Data;

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

        foreach(var classification in seedingContext.FileClassificationCategories)
        {
            seedingContext.FileClassificationCategories.Remove(classification);
        }
        seedingContext.SaveChanges();
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

        var classification = FileClassificationFactory.Create("Photos", Option.None<string>(), Option.None<string>(), false, false);
        var sut = CreateSut(factory);

        var result = await sut.ResolveManyAsync([classification], TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        result[0].ShouldBe(existing.Id);
    }

    [Fact]
    public async Task when_level1_missing_then_creates_and_returns_new_id()
    {
        var (_, factory) = CreateInMemoryFactory();
        var classification = FileClassificationFactory.Create("Photos", Option.None<string>(), Option.None<string>(), false, false);
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

        var classification = FileClassificationFactory.Create("Photos", Option.Some("Holidays"), Option.None<string>(), false, false);
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

        var classification = FileClassificationFactory.Create("Photos", Option.Some("Holidays"), Option.Some("Christmas"), false, false);
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

        var classification = FileClassificationFactory.Create("Photos", Option.Some("Holidays"), Option.None<string>(), false, false);
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
        var classification = FileClassificationFactory.Create("Photos", Option.None<string>(), Option.None<string>(), false, false);
        var sut = CreateSut(factory);

        var result = await sut.ResolveManyAsync([classification, classification], TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task when_all_nodes_pre_existing_then_no_new_rows_are_inserted()
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

        var classification = FileClassificationFactory.Create("Photos", Option.Some("Holidays"), Option.Some("Christmas"), false, false);
        var sut = CreateSut(factory);

        var result = await sut.ResolveManyAsync([classification], TestContext.Current.CancellationToken);

        db.FileClassificationCategories.Count().ShouldBe(3);
        result.ShouldHaveSingleItem();
        result[0].ShouldBe(level3.Id);
    }

    [Fact]
    public async Task when_fully_new_three_level_hierarchy_then_all_nodes_created_with_correct_parent_chain()
    {
        var (db, factory) = CreateInMemoryFactory();
        var classification = FileClassificationFactory.Create("Photos", Option.Some("Holidays"), Option.Some("Christmas"), false, false);
        var sut = CreateSut(factory);

        var result = await sut.ResolveManyAsync([classification], TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        var all = db.FileClassificationCategories.ToList();
        all.Count.ShouldBe(3);
        var l1 = all.Single(c => c.Level == 1);
        var l2 = all.Single(c => c.Level == 2);
        var l3 = all.Single(c => c.Level == 3);
        l1.Name.ShouldBe("Photos");
        l1.ParentId.ShouldBeNull();
        l2.Name.ShouldBe("Holidays");
        l2.ParentId.ShouldBe(l1.Id);
        l3.Name.ShouldBe("Christmas");
        l3.ParentId.ShouldBe(l2.Id);
        result[0].ShouldBe(l3.Id);
    }

    [Fact]
    public async Task when_multiple_classifications_share_new_level1_then_level1_is_created_only_once()
    {
        var (db, factory) = CreateInMemoryFactory();
        var classification1 = FileClassificationFactory.Create("Photos", Option.Some("Holidays"), Option.None<string>(), false, false);
        var classification2 = FileClassificationFactory.Create("Photos", Option.Some("Birthdays"), Option.None<string>(), false, false);
        var sut = CreateSut(factory);

        await sut.ResolveManyAsync([classification1, classification2], TestContext.Current.CancellationToken);

        db.FileClassificationCategories.Count(c => c.Level == 1).ShouldBe(1);
        db.FileClassificationCategories.Count(c => c.Level == 2).ShouldBe(2);
    }
}
