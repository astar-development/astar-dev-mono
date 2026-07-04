using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenAFileClassificationService : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;
    private IDbContextFactory<AppDbContext> factory = null!;
    private FileClassificationService sut = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var seedContext = new AppDbContext(options);
        await seedContext.Database.MigrateAsync();

        seedContext.ScrapeConfiguration.Add(CreateScrapeConfigEntity());
        await seedContext.SaveChangesAsync();

        seedContext.ScrapeConfiguration.Add(CreateScrapeConfigEntity());
        await seedContext.SaveChangesAsync();

        factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
               .Returns(_ => Task.FromResult(new AppDbContext(options)));

        sut = new FileClassificationService(factory);
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_loading_page_classification_data_with_no_classification_seed_data_then_searchable_classifications_are_empty()
    {
        var result = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);

        result.SearchableClassifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_loading_page_classification_data_with_no_classification_seed_data_then_category_classification_is_null()
    {
        var result = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);

        result.CategoryClassification.ShouldBeNull();
    }

    [Fact]
    public async Task when_loading_page_classification_data_with_no_classification_seed_data_then_included_tags_are_empty()
    {
        var result = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);

        result.IncludedTags.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_loading_page_classification_data_then_only_searchable_classifications_are_returned()
    {
        await using var seedCtx = new AppDbContext(options);
        seedCtx.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Included", Level = 3, IncludeInSearch = true });
        seedCtx.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Excluded", Level = 3, IncludeInSearch = false });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);

        result.SearchableClassifications.ShouldHaveSingleItem().Category.Name.ShouldBe("Included");
    }

    [Fact]
    public async Task when_loading_page_classification_data_then_searchable_classification_keywords_are_included()
    {
        await using var seedCtx = new AppDbContext(options);
        var category = new FileClassificationCategoryEntity { Name = "Animals", Level = 3, IncludeInSearch = true };
        seedCtx.FileClassificationCategories.Add(category);
        seedCtx.FileClassificationKeywords.Add(new FileClassificationKeywordEntity { Keyword = "animals", Category = category });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);

        result.SearchableClassifications.ShouldHaveSingleItem().Keywords.ShouldHaveSingleItem().ShouldBe("animals");
    }

    [Fact]
    public async Task when_loading_page_classification_data_with_a_matching_category_then_category_classification_is_not_null()
    {
        await using var seedCtx = new AppDbContext(options);
        seedCtx.ScrapeConfiguration.Add(CreateScrapeConfigEntityWithCategory("cat1", "animals", true));
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("cat1", TestContext.Current.CancellationToken);

        result.CategoryClassification.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_loading_page_classification_data_with_a_matching_category_then_category_classification_name_is_title_cased()
    {
        await using var seedCtx = new AppDbContext(options);
        seedCtx.ScrapeConfiguration.Add(CreateScrapeConfigEntityWithCategory("cat1", "animals", true));
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("cat1", TestContext.Current.CancellationToken);

        result.CategoryClassification!.Name.ShouldBe("Animals");
    }

    [Fact]
    public async Task when_loading_page_classification_data_with_a_category_id_that_does_not_match_any_search_category_then_category_classification_is_null()
    {
        await using var seedCtx = new AppDbContext(options);
        seedCtx.ScrapeConfiguration.Add(CreateScrapeConfigEntityWithCategory("cat1", "animals", true));
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("cat2", TestContext.Current.CancellationToken);

        result.CategoryClassification.ShouldBeNull();
    }

    [Fact]
    public async Task when_loading_page_classification_data_with_category_only_in_older_config_then_category_classification_is_null()
    {
        await using var seedCtx = new AppDbContext(options);
        seedCtx.ScrapeConfiguration.Add(CreateScrapeConfigEntityWithCategory("cat1", "Animals", true));
        seedCtx.ScrapeConfiguration.Add(CreateScrapeConfigEntity());
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("cat1", TestContext.Current.CancellationToken);

        result.CategoryClassification.ShouldBeNull();
    }

    [Fact]
    public async Task when_loading_page_classification_data_then_only_included_scraped_tags_are_returned()
    {
        await using var seedCtx = new AppDbContext(options);
        seedCtx.ScrapedTags.Add(new ScrapedTagEntity { Value = "included-tag", IncludeInSearch = true });
        seedCtx.ScrapedTags.Add(new ScrapedTagEntity { Value = "excluded-tag", IncludeInSearch = false });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);

        result.IncludedTags.ShouldHaveSingleItem().Value.ShouldBe("included-tag");
    }

    [Fact]
    public async Task when_classifying_with_page_data_containing_a_non_null_category_classification_then_a_downloaded_file_classification_is_saved()
    {
        await using var seedCtx = new AppDbContext(options);
        var classification = new FileClassificationCategoryEntity { Name = "Animals", Level = 3, IncludeInSearch = true };
        seedCtx.FileClassificationCategories.Add(classification);
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp"),
            FileHandle = FileHandleFactory.Create("test-handle-category")
        };
        seedCtx.Files.Add(fileDetail);
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pageData = new PageClassificationData([], classification, []);

        await sut.ClassifyAsync(fileDetail, pageData, [], TestContext.Current.CancellationToken);

        await using var verifyCtx = new AppDbContext(options);
        int count = await verifyCtx.FileClassifications.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task when_classifying_with_empty_page_data_then_no_downloaded_file_classifications_are_saved()
    {
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp")
        };
        var pageData = new PageClassificationData([], null, []);

        await sut.ClassifyAsync(fileDetail, pageData, [], TestContext.Current.CancellationToken);

        await using var verifyCtx = new AppDbContext(options);
        int count = await verifyCtx.FileClassifications.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(0);
    }

    [Fact]
    public async Task when_classifying_with_page_data_containing_a_matching_filename_part_then_that_classification_is_recorded()
    {
        await using var seedCtx = new AppDbContext(options);
        var classification = new FileClassificationCategoryEntity { Name = "Animals", Level = 3, IncludeInSearch = true };
        seedCtx.FileClassificationCategories.Add(classification);
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName("animals-photo.jpg"),
            DirectoryName = new DirectoryName("/tmp"),
            FileHandle = FileHandleFactory.Create("test-handle-filename")
        };
        seedCtx.Files.Add(fileDetail);
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pageData = new PageClassificationData([(classification, ["animals"])], null, []);

        await sut.ClassifyAsync(fileDetail, pageData, [], TestContext.Current.CancellationToken);

        await using var verifyCtx = new AppDbContext(options);
        int count = await verifyCtx.FileClassifications.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task when_classifying_with_a_matching_image_tag_then_that_classification_is_recorded()
    {
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp"),
            FileHandle = FileHandleFactory.Create("test-handle-tag")
        };
        await using var seedCtx = new AppDbContext(options);
        seedCtx.Files.Add(fileDetail);
        seedCtx.ScrapedTags.Add(new ScrapedTagEntity { Value = "outdoors", IncludeInSearch = true });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pageData = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);

        await sut.ClassifyAsync(fileDetail, pageData, ["outdoors"], TestContext.Current.CancellationToken);

        await using var verifyCtx = new AppDbContext(options);
        int count = await verifyCtx.FileClassifications.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task when_classifying_a_file_that_already_has_classifications_then_no_additional_rows_are_added()
    {
        await using var seedCtx = new AppDbContext(options);
        var existingCategory = new FileClassificationCategoryEntity { Name = "Existing", Level = 3, IncludeInSearch = true };
        var newCategory = new FileClassificationCategoryEntity { Name = "Animals", Level = 3, IncludeInSearch = true };
        seedCtx.FileClassificationCategories.AddRange(existingCategory, newCategory);
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp"),
            FileHandle = FileHandleFactory.Create("test-handle-already-classified")
        };
        seedCtx.Files.Add(fileDetail);
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);
        seedCtx.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = existingCategory.Id });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pageData = new PageClassificationData([], newCategory, []);

        await sut.ClassifyAsync(fileDetail, pageData, [], TestContext.Current.CancellationToken);

        await using var verifyCtx = new AppDbContext(options);
        int count = await verifyCtx.FileClassifications.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task when_classifying_with_a_tag_matching_an_existing_hierarchy_category_then_the_existing_category_is_reused()
    {
        await using var seedCtx = new AppDbContext(options);
        var parent = new FileClassificationCategoryEntity { Name = "Bum", Level = 1 };
        seedCtx.FileClassificationCategories.Add(parent);
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var existing = new FileClassificationCategoryEntity { Name = "Ass", Level = 2, ParentId = parent.Id };
        seedCtx.FileClassificationCategories.Add(existing);
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp"),
            FileHandle = FileHandleFactory.Create("test-handle-hierarchy-reuse")
        };
        seedCtx.Files.Add(fileDetail);
        seedCtx.ScrapedTags.Add(new ScrapedTagEntity { Value = "ass", IncludeInSearch = true });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pageData = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);
        await sut.ClassifyAsync(fileDetail, pageData, ["ass"], TestContext.Current.CancellationToken);

        await using var verifyCtx = new AppDbContext(options);
        int categoryCount = await verifyCtx.FileClassificationCategories.CountAsync(c => c.Name == "Ass", TestContext.Current.CancellationToken);
        var junction = await verifyCtx.FileClassifications.SingleAsync(TestContext.Current.CancellationToken);
        categoryCount.ShouldBe(1);
        junction.CategoryId.ShouldBe(existing.Id);
    }

    [Fact]
    public async Task when_classifying_with_a_tag_matching_multiple_existing_categories_then_the_deepest_level_category_is_reused()
    {
        await using var seedCtx = new AppDbContext(options);
        var levelOne = new FileClassificationCategoryEntity { Name = "Boobs", Level = 1 };
        seedCtx.FileClassificationCategories.Add(levelOne);
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var levelTwo = new FileClassificationCategoryEntity { Name = "Boobs", Level = 2, ParentId = levelOne.Id };
        seedCtx.FileClassificationCategories.Add(levelTwo);
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp"),
            FileHandle = FileHandleFactory.Create("test-handle-deepest-reuse")
        };
        seedCtx.Files.Add(fileDetail);
        seedCtx.ScrapedTags.Add(new ScrapedTagEntity { Value = "boobs", IncludeInSearch = true });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pageData = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);
        await sut.ClassifyAsync(fileDetail, pageData, ["boobs"], TestContext.Current.CancellationToken);

        await using var verifyCtx = new AppDbContext(options);
        var junction = await verifyCtx.FileClassifications.SingleAsync(TestContext.Current.CancellationToken);
        junction.CategoryId.ShouldBe(levelTwo.Id);
    }

    [Fact]
    public async Task when_classifying_with_a_tag_matching_an_existing_category_with_different_casing_then_no_duplicate_category_is_created()
    {
        await using var seedCtx = new AppDbContext(options);
        var parent = new FileClassificationCategoryEntity { Name = "Clothes", Level = 1 };
        seedCtx.FileClassificationCategories.Add(parent);
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var existing = new FileClassificationCategoryEntity { Name = "BATHROBES", Level = 2, ParentId = parent.Id };
        seedCtx.FileClassificationCategories.Add(existing);
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp"),
            FileHandle = FileHandleFactory.Create("test-handle-casing-reuse")
        };
        seedCtx.Files.Add(fileDetail);
        seedCtx.ScrapedTags.Add(new ScrapedTagEntity { Value = "bathrobes", IncludeInSearch = true });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pageData = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);
        await sut.ClassifyAsync(fileDetail, pageData, ["bathrobes"], TestContext.Current.CancellationToken);

        await using var verifyCtx = new AppDbContext(options);
        int categoryCount = await verifyCtx.FileClassificationCategories.CountAsync(c => EF.Functions.Collate(c.Name, "NOCASE") == "Bathrobes", TestContext.Current.CancellationToken);
        var junction = await verifyCtx.FileClassifications.SingleAsync(TestContext.Current.CancellationToken);
        categoryCount.ShouldBe(1);
        junction.CategoryId.ShouldBe(existing.Id);
    }

    [Fact]
    public async Task when_classifying_with_a_tag_matching_no_existing_category_then_a_level_two_category_under_the_unclassified_root_is_created()
    {
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp"),
            FileHandle = FileHandleFactory.Create("test-handle-new-tag")
        };
        await using var seedCtx = new AppDbContext(options);
        seedCtx.Files.Add(fileDetail);
        seedCtx.ScrapedTags.Add(new ScrapedTagEntity { Value = "brand new tag", IncludeInSearch = true });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pageData = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);
        await sut.ClassifyAsync(fileDetail, pageData, ["brand new tag"], TestContext.Current.CancellationToken);

        await using var verifyCtx = new AppDbContext(options);
        var created = await verifyCtx.FileClassificationCategories.SingleAsync(c => c.Name == "Brand New Tag", TestContext.Current.CancellationToken);
        var root = await verifyCtx.FileClassificationCategories.SingleAsync(c => c.Name == "Unclassified", TestContext.Current.CancellationToken);
        created.Level.ShouldBe(2);
        created.ParentId.ShouldBe(root.Id);
        root.Level.ShouldBe(1);
        root.ParentId.ShouldBeNull();
    }

    [Fact]
    public async Task when_classifying_with_a_new_tag_and_the_unclassified_root_already_exists_then_it_is_reused()
    {
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp"),
            FileHandle = FileHandleFactory.Create("test-handle-existing-root")
        };
        await using var seedCtx = new AppDbContext(options);
        var root = new FileClassificationCategoryEntity { Name = "Unclassified", Level = 1 };
        seedCtx.FileClassificationCategories.Add(root);
        seedCtx.Files.Add(fileDetail);
        seedCtx.ScrapedTags.Add(new ScrapedTagEntity { Value = "another new tag", IncludeInSearch = true });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pageData = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);
        await sut.ClassifyAsync(fileDetail, pageData, ["another new tag"], TestContext.Current.CancellationToken);

        await using var verifyCtx = new AppDbContext(options);
        var created = await verifyCtx.FileClassificationCategories.SingleAsync(c => c.Name == "Another New Tag", TestContext.Current.CancellationToken);
        int rootCount = await verifyCtx.FileClassificationCategories.CountAsync(c => c.Name == "Unclassified", TestContext.Current.CancellationToken);
        created.Level.ShouldBe(2);
        created.ParentId.ShouldBe(root.Id);
        rootCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_classifying_with_two_new_tags_then_both_share_a_single_unclassified_root()
    {
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp"),
            FileHandle = FileHandleFactory.Create("test-handle-two-new-tags")
        };
        await using var seedCtx = new AppDbContext(options);
        seedCtx.Files.Add(fileDetail);
        seedCtx.ScrapedTags.Add(new ScrapedTagEntity { Value = "first new tag", IncludeInSearch = true });
        seedCtx.ScrapedTags.Add(new ScrapedTagEntity { Value = "second new tag", IncludeInSearch = true });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pageData = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);
        await sut.ClassifyAsync(fileDetail, pageData, ["first new tag", "second new tag"], TestContext.Current.CancellationToken);

        await using var verifyCtx = new AppDbContext(options);
        int rootCount = await verifyCtx.FileClassificationCategories.CountAsync(c => c.Name == "Unclassified", TestContext.Current.CancellationToken);
        int childCount = await verifyCtx.FileClassificationCategories.CountAsync(c => c.Level == 2 && c.Name != "Unclassified", TestContext.Current.CancellationToken);
        rootCount.ShouldBe(1);
        childCount.ShouldBe(2);
    }

    [Fact]
    public async Task when_resolving_the_category_classification_matching_an_existing_hierarchy_category_then_the_existing_category_is_reused()
    {
        await using var seedCtx = new AppDbContext(options);
        var parent = new FileClassificationCategoryEntity { Name = "Person", Level = 1 };
        seedCtx.FileClassificationCategories.Add(parent);
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var existing = new FileClassificationCategoryEntity { Name = "Animals", Level = 2, ParentId = parent.Id };
        seedCtx.FileClassificationCategories.Add(existing);
        seedCtx.ScrapeConfiguration.Add(CreateScrapeConfigEntityWithCategory("cat1", "animals", true));
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("cat1", TestContext.Current.CancellationToken);

        result.CategoryClassification.ShouldNotBeNull();
        result.CategoryClassification.Id.ShouldBe(existing.Id);
    }

    [Fact]
    public async Task when_exporting_classifications_then_categories_and_keywords_are_returned()
    {
        await using var seedCtx = new AppDbContext(options);
        var classification = new FileClassificationCategoryEntity { Name = "Animals", Level = 3, IncludeInSearch = true };
        seedCtx.FileClassificationCategories.Add(classification);
        seedCtx.FileClassificationKeywords.Add(new FileClassificationKeywordEntity { Keyword = "animals", Category = classification });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.ExportClassificationsAsync(TestContext.Current.CancellationToken);

        result.Categories.ShouldContain(c => c.Name == "Animals");
        result.Keywords.ShouldHaveSingleItem().Keyword.ShouldBe("animals");
    }

    [Fact]
    public async Task when_importing_a_new_classification_then_it_is_added()
    {
        var incoming = (
            Categories: new List<FileClassificationCategoryEntity> { new() { Id = 1, Name = "Animals", Level = 3, IncludeInSearch = true } },
            Keywords: new List<FileClassificationKeywordEntity> { new() { CategoryId = 1, Keyword = "animals" } });

        await sut.ImportClassificationsAsync(incoming, TestContext.Current.CancellationToken);

        await using var verifyCtx = new AppDbContext(options);
        var stored = await verifyCtx.FileClassificationCategories.SingleAsync(c => c.Name == "Animals", TestContext.Current.CancellationToken);
        stored.Name.ShouldBe("Animals");
    }

    [Fact]
    public async Task when_importing_a_classification_whose_keyword_already_exists_with_different_casing_then_no_duplicate_keyword_is_added()
    {
        await using var seedCtx = new AppDbContext(options);
        var existing = new FileClassificationCategoryEntity { Name = "Animals", Level = 3, IncludeInSearch = true };
        seedCtx.FileClassificationCategories.Add(existing);
        seedCtx.FileClassificationKeywords.Add(new FileClassificationKeywordEntity { Keyword = "ANIMALS", Category = existing });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var incoming = (
            Categories: new List<FileClassificationCategoryEntity> { new() { Id = existing.Id, Name = "Animals", Level = 3, IncludeInSearch = true } },
            Keywords: new List<FileClassificationKeywordEntity> { new() { CategoryId = existing.Id, Keyword = "animals" } });

        await sut.ImportClassificationsAsync(incoming, TestContext.Current.CancellationToken);

        await using var verifyCtx = new AppDbContext(options);
        int count = await verifyCtx.FileClassificationKeywords.CountAsync(k => k.CategoryId == existing.Id, TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    private static ScrapeConfigurationEntity CreateScrapeConfigEntity() => new()
    {
        ConnectionStrings = new ConnectionStringsEntity { Sqlite = "Data Source=test.db" },
        UserConfiguration = new UserConfigurationEntity { LoginEmailAddress = "user@example.com", Username = "user", Password = "password", SessionCookie = "cookie" },
        SearchConfiguration = new SearchConfigurationEntity { BaseUrl = new Uri("https://example.com") },
        ScrapeDirectories = new ScrapeDirectoriesEntity { RootDirectory = "/tmp" }
    };

    private static ScrapeConfigurationEntity CreateScrapeConfigEntityWithCategory(string categoryId, string categoryName, bool includeInSearch)
    {
        var entity = new ScrapeConfigurationEntity
        {
            ConnectionStrings = new ConnectionStringsEntity { Sqlite = "Data Source=test.db" },
            UserConfiguration = new UserConfigurationEntity { LoginEmailAddress = "user@example.com", Username = "user", Password = "password", SessionCookie = "cookie" },
            SearchConfiguration = new SearchConfigurationEntity { BaseUrl = new Uri("https://example.com") },
            ScrapeDirectories = new ScrapeDirectoriesEntity { RootDirectory = "/tmp" }
        };
        entity.SearchConfiguration.SearchCategories.Add(new SearchCategoryEntity { Id = categoryId, Name = categoryName, IncludeInSearch = includeInSearch });

        return entity;
    }
}
