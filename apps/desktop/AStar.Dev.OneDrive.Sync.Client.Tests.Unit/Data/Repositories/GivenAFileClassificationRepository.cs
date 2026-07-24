using AStar.Dev.Functional.Extensions;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
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

    private static ILogger<FileClassificationRepository> CreateLogger()
    {
        var logger = Substitute.For<ILogger<FileClassificationRepository>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        return logger;
    }

    private static async Task<FileDetailEntity> SeedFileDetailAsync(AppDbContext db)
    {
        var fileDetail = new FileDetailEntity { FileName = new FileName("photo.jpg"), DirectoryName = new DirectoryName("/local"), FileHandle = new FileHandle("handle-classification-repo") };
        db.Files.Add(fileDetail);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return fileDetail;
    }

    [Fact]
    public async Task when_has_classifications_is_called_for_an_unclassified_file_then_false_is_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());
        var fileDetail = await SeedFileDetailAsync(db);

        bool hasClassifications = await repository.HasClassificationsAsync(fileDetail.Id, TestContext.Current.CancellationToken);

        hasClassifications.ShouldBeFalse();
    }

    [Fact]
    public async Task when_has_classifications_is_called_for_a_classified_file_then_true_is_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());
        var fileDetail = await SeedFileDetailAsync(db);
        var category = new FileClassificationCategoryEntity { Name = "Photos", Level = 1 };
        db.FileClassificationCategories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        bool hasClassifications = await repository.HasClassificationsAsync(fileDetail.Id, TestContext.Current.CancellationToken);

        hasClassifications.ShouldBeTrue();
    }

    [Fact]
    public async Task when_add_classifications_is_called_then_one_row_per_category_is_persisted()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());
        var fileDetail = await SeedFileDetailAsync(db);
        var photos = new FileClassificationCategoryEntity { Name = "Photos", Level = 1 };
        var media = new FileClassificationCategoryEntity { Name = "Media", Level = 1 };
        db.FileClassificationCategories.AddRange(photos, media);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.AddClassificationsAsync(fileDetail.Id, [photos.Id, media.Id], TestContext.Current.CancellationToken);

        var rows = db.FileClassifications.Where(c => c.FileDetailId == fileDetail.Id).ToList();
        rows.Count.ShouldBe(2);
        rows.ShouldContain(r => r.CategoryId == photos.Id);
        rows.ShouldContain(r => r.CategoryId == media.Id);
    }

    [Fact]
    public async Task when_add_classifications_is_called_with_an_empty_list_then_no_rows_are_persisted()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());
        var fileDetail = await SeedFileDetailAsync(db);

        await repository.AddClassificationsAsync(fileDetail.Id, [], TestContext.Current.CancellationToken);

        db.FileClassifications.Any().ShouldBeFalse();
    }

    [Fact]
    public async Task when_get_all_categories_contains_one_valid_and_one_invalid_row_then_only_valid_row_is_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());

        db.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Photos", Level = 1, IncludeInSearch = true });
        db.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "", Level = 1, IncludeInSearch = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetAllCategoriesAsync(TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Photos");
    }

    [Fact]
    public async Task when_get_all_categories_contains_only_valid_rows_then_all_are_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());

        db.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Photos", Level = 1, IncludeInSearch = true });
        db.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Documents", Level = 1, IncludeInSearch = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetAllCategoriesAsync(TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_get_all_categories_contains_only_invalid_rows_then_empty_list_is_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());

        db.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "", Level = 1, IncludeInSearch = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetAllCategoriesAsync(TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_get_all_categories_contains_invalid_row_then_logger_receives_a_warning_call()
    {
        var (db, factory) = CreateInMemoryFactory();
        var logger = CreateLogger();
        var repository = new FileClassificationRepository(factory, logger);

        db.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "", Level = 1, IncludeInSearch = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.GetAllCategoriesAsync(TestContext.Current.CancellationToken);

        logger.ReceivedCalls().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_get_all_categories_is_called_with_no_rows_then_empty_list_is_returned()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());

        var result = await repository.GetAllCategoriesAsync(TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_add_category_is_called_with_include_in_search_true_then_it_is_persisted()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());
        var createResult = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(0), "Photos", 1, false, false, Option.None<FileClassificationCategoryId>(), true);
        var category = ((Result<FileClassificationCategory, string>.Ok)createResult).Value;

        var addResult = await repository.AddCategoryAsync(category, TestContext.Current.CancellationToken);

        addResult.ShouldBeOfType<Result<FileClassificationCategoryId, string>.Ok>();
        var persisted = db.FileClassificationCategories.Single(c => c.Name == "Photos");
        persisted.IncludeInSearch.ShouldBeTrue();
    }

    [Fact]
    public async Task when_update_category_is_called_with_include_in_search_true_then_it_is_persisted()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());
        var existing = new FileClassificationCategoryEntity { Name = "Photos", Level = 1, IncludeInSearch = false };
        db.FileClassificationCategories.Add(existing);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var createResult = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(existing.Id), "Photos", 1, false, false, Option.None<FileClassificationCategoryId>(), true);
        var category = ((Result<FileClassificationCategory, string>.Ok)createResult).Value;

        var updateResult = await repository.UpdateCategoryAsync(new FileClassificationCategoryId(existing.Id), category, TestContext.Current.CancellationToken);

        updateResult.ShouldBeOfType<Result<FileClassificationCategoryId, string>.Ok>();
        var persisted = db.FileClassificationCategories.AsNoTracking().Single(c => c.Id == existing.Id);
        persisted.IncludeInSearch.ShouldBeTrue();
    }
}
