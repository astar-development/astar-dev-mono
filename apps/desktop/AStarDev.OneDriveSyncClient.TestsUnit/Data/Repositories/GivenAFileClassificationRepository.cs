using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Data.Repositories;

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
    public async Task when_get_all_categories_contains_a_row_with_include_in_search_false_then_that_row_is_still_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());

        db.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Archived", Level = 1, IncludeInSearch = false });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetAllCategoriesAsync(TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Archived");
        result[0].IncludeInSearch.ShouldBeFalse();
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
        var category = ((Ok<FileClassificationCategory, string>)createResult).Value;

        var addResult = await repository.AddCategoryAsync(category, TestContext.Current.CancellationToken);

        addResult.ShouldBeOfType<Ok<FileClassificationCategoryId, string>>();
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
        var category = ((Ok<FileClassificationCategory, string>)createResult).Value;

        var updateResult = await repository.UpdateCategoryAsync(new FileClassificationCategoryId(existing.Id), category, TestContext.Current.CancellationToken);

        updateResult.ShouldBeOfType<Ok<FileClassificationCategoryId, string>>();
        var persisted = db.FileClassificationCategories.AsNoTracking().Single(c => c.Id == existing.Id);
        persisted.IncludeInSearch.ShouldBeTrue();
    }

    [Fact]
    public async Task when_reparent_category_is_called_with_a_valid_parent_then_level_is_recalculated()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());
        var media = new FileClassificationCategoryEntity { Name = "Media", Level = 1 };
        var documents = new FileClassificationCategoryEntity { Name = "Documents", Level = 1 };
        db.FileClassificationCategories.AddRange(media, documents);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.ReparentCategoryAsync(new FileClassificationCategoryId(documents.Id), Option.Some(new FileClassificationCategoryId(media.Id)), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<FileClassificationCategoryId, string>>();
        var persisted = db.FileClassificationCategories.AsNoTracking().Single(c => c.Id == documents.Id);
        persisted.Level.ShouldBe(2);
        persisted.ParentId.ShouldBe(media.Id);
    }

    [Fact]
    public async Task when_reparent_category_is_called_then_level_change_cascades_to_descendants()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());
        var media = new FileClassificationCategoryEntity { Name = "Media", Level = 1 };
        var documents = new FileClassificationCategoryEntity { Name = "Documents", Level = 1 };
        db.FileClassificationCategories.AddRange(media, documents);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var photos = new FileClassificationCategoryEntity { Name = "Photos", Level = 2, ParentId = documents.Id };
        db.FileClassificationCategories.Add(photos);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var holiday = new FileClassificationCategoryEntity { Name = "Holiday", Level = 3, ParentId = photos.Id };
        db.FileClassificationCategories.Add(holiday);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.ReparentCategoryAsync(new FileClassificationCategoryId(documents.Id), Option.Some(new FileClassificationCategoryId(media.Id)), TestContext.Current.CancellationToken);

        db.FileClassificationCategories.AsNoTracking().Single(c => c.Id == photos.Id).Level.ShouldBe(3);
        db.FileClassificationCategories.AsNoTracking().Single(c => c.Id == holiday.Id).Level.ShouldBe(4);
    }

    [Fact]
    public async Task when_reparent_category_to_root_then_level_becomes_one_and_parent_cleared()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());
        var media = new FileClassificationCategoryEntity { Name = "Media", Level = 1 };
        db.FileClassificationCategories.Add(media);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var photos = new FileClassificationCategoryEntity { Name = "Photos", Level = 2, ParentId = media.Id };
        db.FileClassificationCategories.Add(photos);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.ReparentCategoryAsync(new FileClassificationCategoryId(photos.Id), Option.None<FileClassificationCategoryId>(), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<FileClassificationCategoryId, string>>();
        var persisted = db.FileClassificationCategories.AsNoTracking().Single(c => c.Id == photos.Id);
        persisted.Level.ShouldBe(1);
        persisted.ParentId.ShouldBeNull();
    }

    [Fact]
    public async Task when_reparent_category_under_its_own_descendant_then_result_is_failure()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());
        var media = new FileClassificationCategoryEntity { Name = "Media", Level = 1 };
        db.FileClassificationCategories.Add(media);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var photos = new FileClassificationCategoryEntity { Name = "Photos", Level = 2, ParentId = media.Id };
        db.FileClassificationCategories.Add(photos);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.ReparentCategoryAsync(new FileClassificationCategoryId(media.Id), Option.Some(new FileClassificationCategoryId(photos.Id)), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Fail<FileClassificationCategoryId, string>>();
    }

    [Fact]
    public async Task when_reparent_category_under_itself_then_result_is_failure()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());
        var media = new FileClassificationCategoryEntity { Name = "Media", Level = 1 };
        db.FileClassificationCategories.Add(media);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.ReparentCategoryAsync(new FileClassificationCategoryId(media.Id), Option.Some(new FileClassificationCategoryId(media.Id)), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Fail<FileClassificationCategoryId, string>>();
    }

    [Fact]
    public async Task when_reparent_category_with_nonexistent_parent_then_result_is_failure()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());
        var media = new FileClassificationCategoryEntity { Name = "Media", Level = 1 };
        db.FileClassificationCategories.Add(media);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.ReparentCategoryAsync(new FileClassificationCategoryId(media.Id), Option.Some(new FileClassificationCategoryId(9999)), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Fail<FileClassificationCategoryId, string>>();
    }

    [Fact]
    public async Task when_reparent_category_that_does_not_exist_then_result_is_failure()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());

        var result = await repository.ReparentCategoryAsync(new FileClassificationCategoryId(9999), Option.None<FileClassificationCategoryId>(), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Fail<FileClassificationCategoryId, string>>();
    }

    [Fact]
    public async Task when_add_category_is_called_with_an_already_cancelled_token_then_the_db_context_factory_is_never_called()
    {
        var (_, factory) = CreateInMemoryFactory();
        var repository = new FileClassificationRepository(factory, CreateLogger());
        var createResult = FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(0), "Photos", 1, false, false, Option.None<FileClassificationCategoryId>(), true);
        var category = ((Ok<FileClassificationCategory, string>)createResult).Value;
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => repository.AddCategoryAsync(category, cancellationTokenSource.Token));

        await factory.DidNotReceive().CreateDbContextAsync(Arg.Any<CancellationToken>());
    }
}
