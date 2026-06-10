using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Repositories;

public sealed class GivenAFileClassificationRepository
{
    private static (AppDbContext seedingContext, IDbContextFactory<AppDbContext> factory) CreateInMemoryFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var seedingContext = new AppDbContext(options);
        _ = seedingContext.Database.EnsureCreated();
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(callInfo => Task.FromResult(new AppDbContext(options)));

        return (seedingContext, factory);
    }

    [Fact]
    public async Task when_get_all_categories_contains_one_valid_and_one_invalid_row_then_only_valid_row_is_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var logger = Substitute.For<ILogger<FileClassificationRepository>>();
        var repository = new FileClassificationRepository(factory, logger);

        db.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Photos", Level = 1 });
        db.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "", Level = 1 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetAllCategoriesAsync(TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Photos");
    }

    [Fact]
    public async Task when_get_all_categories_contains_only_valid_rows_then_all_are_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var logger = Substitute.For<ILogger<FileClassificationRepository>>();
        var repository = new FileClassificationRepository(factory, logger);

        db.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Photos", Level = 1 });
        db.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Documents", Level = 1 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetAllCategoriesAsync(TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_get_all_categories_contains_only_invalid_rows_then_empty_list_is_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var logger = Substitute.For<ILogger<FileClassificationRepository>>();
        var repository = new FileClassificationRepository(factory, logger);

        db.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "", Level = 1 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetAllCategoriesAsync(TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_get_all_categories_contains_invalid_row_then_warning_is_logged()
    {
        var (db, factory) = CreateInMemoryFactory();
        var warnings = new List<(int entityId, string error)>();
        var logger = Substitute.For<ILogger<FileClassificationRepository>>();
        logger.When(l => OneDriveSyncClientMessages.ClassificationRowSkipped(l, Arg.Any<int>(), Arg.Any<string>()))
              .Do(ci => warnings.Add((ci.ArgAt<int>(1), ci.ArgAt<string>(2))));
        var repository = new FileClassificationRepository(factory, logger);

        db.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "", Level = 1 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.GetAllCategoriesAsync(TestContext.Current.CancellationToken);

        warnings.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_get_all_categories_is_called_with_no_rows_then_empty_list_is_returned()
    {
        var (_, factory) = CreateInMemoryFactory();
        var logger = Substitute.For<ILogger<FileClassificationRepository>>();
        var repository = new FileClassificationRepository(factory, logger);

        var result = await repository.GetAllCategoriesAsync(TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }
}
