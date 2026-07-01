using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenAFileClassificationService : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private DbContextOptions<FilesContext> options = null!;
    private IDbContextFactory<FilesContext> factory = null!;
    private FileClassificationService sut = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<FilesContext>()
            .UseSqlite(connection)
            .Options;

        await using var seedContext = new FilesContext(options);
        await seedContext.Database.MigrateAsync();

        seedContext.ScrapeConfiguration.Add(CreateScrapeConfigEntity());
        await seedContext.SaveChangesAsync();

        seedContext.ScrapeConfiguration.Add(CreateScrapeConfigEntity());
        await seedContext.SaveChangesAsync();

        factory = Substitute.For<IDbContextFactory<FilesContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
               .Returns(_ => Task.FromResult(new FilesContext(options)));

        sut = new FileClassificationService(factory, new FakeTimeProvider());
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
        await using var seedCtx = new FilesContext(options);
        seedCtx.FileClassifications.Add(new FileClassification { Name = "Included", IncludeInSearch = true });
        seedCtx.FileClassifications.Add(new FileClassification { Name = "Excluded", IncludeInSearch = false });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);

        result.SearchableClassifications.ShouldHaveSingleItem().Name.ShouldBe("Included");
    }

    [Fact]
    public async Task when_loading_page_classification_data_with_a_matching_category_then_category_classification_is_not_null()
    {
        await using var seedCtx = new FilesContext(options);
        seedCtx.ScrapeConfiguration.Add(CreateScrapeConfigEntityWithCategory("cat1", "animals", true));
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("cat1", TestContext.Current.CancellationToken);

        result.CategoryClassification.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_loading_page_classification_data_with_a_matching_category_then_category_classification_name_is_title_cased()
    {
        await using var seedCtx = new FilesContext(options);
        seedCtx.ScrapeConfiguration.Add(CreateScrapeConfigEntityWithCategory("cat1", "animals", true));
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("cat1", TestContext.Current.CancellationToken);

        result.CategoryClassification!.Name.ShouldBe("Animals");
    }

    [Fact]
    public async Task when_loading_page_classification_data_with_a_category_id_that_does_not_match_any_search_category_then_category_classification_is_null()
    {
        await using var seedCtx = new FilesContext(options);
        seedCtx.ScrapeConfiguration.Add(CreateScrapeConfigEntityWithCategory("cat1", "animals", true));
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("cat2", TestContext.Current.CancellationToken);

        result.CategoryClassification.ShouldBeNull();
    }

    [Fact]
    public async Task when_loading_page_classification_data_with_category_only_in_older_config_then_category_classification_is_null()
    {
        await using var seedCtx = new FilesContext(options);
        seedCtx.ScrapeConfiguration.Add(CreateScrapeConfigEntityWithCategory("cat1", "Animals", true));
        seedCtx.ScrapeConfiguration.Add(CreateScrapeConfigEntity());
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("cat1", TestContext.Current.CancellationToken);

        result.CategoryClassification.ShouldBeNull();
    }

    [Fact]
    public async Task when_loading_page_classification_data_then_only_included_scraped_tags_are_returned()
    {
        await using var seedCtx = new FilesContext(options);
        seedCtx.ScrapedTags.Add(new ScrapedTag { Value = "included-tag", IncludeInSearch = true });
        seedCtx.ScrapedTags.Add(new ScrapedTag { Value = "excluded-tag", IncludeInSearch = false });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);

        result.IncludedTags.ShouldHaveSingleItem().Value.ShouldBe("included-tag");
    }

    [Fact]
    public async Task when_classifying_with_page_data_containing_a_non_null_category_classification_then_a_downloaded_file_classification_is_saved()
    {
        await using var seedCtx = new FilesContext(options);
        var classification = new FileClassification { Name = "Animals", IncludeInSearch = true };
        seedCtx.FileClassifications.Add(classification);
        var fileDetail = new FileDetail
        {
            FileName = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp"),
            FileHandle = new FileHandle("test-handle-category")
        };
        seedCtx.Files.Add(fileDetail);
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pageData = new PageClassificationData([], classification, []);

        await sut.ClassifyAsync(fileDetail, pageData, [], TestContext.Current.CancellationToken);

        await using var verifyCtx = new FilesContext(options);
        var count = await verifyCtx.DownloadedFileClassifications.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task when_classifying_with_empty_page_data_then_no_downloaded_file_classifications_are_saved()
    {
        var fileDetail = new FileDetail
        {
            FileName = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp")
        };
        var pageData = new PageClassificationData([], null, []);

        await sut.ClassifyAsync(fileDetail, pageData, [], TestContext.Current.CancellationToken);

        await using var verifyCtx = new FilesContext(options);
        var count = await verifyCtx.DownloadedFileClassifications.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(0);
    }

    [Fact]
    public async Task when_classifying_with_page_data_containing_a_matching_filename_part_then_that_classification_is_recorded()
    {
        await using var seedCtx = new FilesContext(options);
        var classification = new FileClassification
        {
            Name = "Animals",
            IncludeInSearch = true
        };
        classification.FileNameParts.Add(new FileNamePart { Text = "animals" });
        seedCtx.FileClassifications.Add(classification);
        var fileDetail = new FileDetail
        {
            FileName = new FileName("animals-photo.jpg"),
            DirectoryName = new DirectoryName("/tmp"),
            FileHandle = new FileHandle("test-handle-filename")
        };
        seedCtx.Files.Add(fileDetail);
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pageData = new PageClassificationData([classification], null, []);

        await sut.ClassifyAsync(fileDetail, pageData, [], TestContext.Current.CancellationToken);

        await using var verifyCtx = new FilesContext(options);
        var count = await verifyCtx.DownloadedFileClassifications.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    private static ScrapeConfigurationEntity CreateScrapeConfigEntity() => new()
    {
        ConnectionStrings = new ConnectionStrings { Sqlite = "Data Source=test.db" },
        UserConfiguration = new UserConfiguration { LoginEmailAddress = "user@example.com", Username = "user", Password = "password", SessionCookie = "cookie" },
        SearchConfiguration = new SearchConfiguration { BaseUrl = new Uri("https://example.com") },
        ScrapeDirectories = new ScrapeDirectories { RootDirectory = "/tmp" }
    };

    private static ScrapeConfigurationEntity CreateScrapeConfigEntityWithCategory(string categoryId, string categoryName, bool includeInSearch) => new()
    {
        ConnectionStrings = new ConnectionStrings { Sqlite = "Data Source=test.db" },
        UserConfiguration = new UserConfiguration { LoginEmailAddress = "user@example.com", Username = "user", Password = "password", SessionCookie = "cookie" },
        SearchConfiguration = new SearchConfiguration
        {
            BaseUrl = new Uri("https://example.com")
        },
        ScrapeDirectories = new ScrapeDirectories { RootDirectory = "/tmp" }
    };
}
