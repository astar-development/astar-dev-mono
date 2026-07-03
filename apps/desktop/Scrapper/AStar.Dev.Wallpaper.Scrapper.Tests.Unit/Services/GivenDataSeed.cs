using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenDataSeed : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private AppDbContext context = null!;
    private string csvPath = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        context = new AppDbContext(options);
        await context.Database.MigrateAsync();

        csvPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(csvPath, "FileNameContains,DatabaseMapping,Celebrity,Searchable\nanimals,Animals,FALSE,TRUE\n");
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
        await connection.DisposeAsync();
        File.Delete(csvPath);
    }

    [Fact]
    public async Task when_seeding_file_classifications_from_csv_then_the_created_classification_has_level_three()
    {
        var logger = new LoggerConfiguration().CreateLogger();

        await DataSeed.SeedFileClassificationsAsync(csvPath, logger, context);

        var classification = await context.FileClassificationCategories.SingleAsync(c => c.Name == "Animals", TestContext.Current.CancellationToken);
        classification.Level.ShouldBe(3);
    }

    [Fact]
    public async Task when_seeding_file_classifications_from_csv_then_the_created_classification_has_a_keyword()
    {
        var logger = new LoggerConfiguration().CreateLogger();

        await DataSeed.SeedFileClassificationsAsync(csvPath, logger, context);

        var keyword = await context.FileClassificationKeywords.SingleAsync(TestContext.Current.CancellationToken);
        keyword.Keyword.ShouldBe("animals");
    }

    [Fact]
    public async Task when_seeding_file_classifications_from_csv_then_the_keyword_is_linked_to_the_created_category()
    {
        var logger = new LoggerConfiguration().CreateLogger();

        await DataSeed.SeedFileClassificationsAsync(csvPath, logger, context);

        var category = await context.FileClassificationCategories.SingleAsync(c => c.Name == "Animals", TestContext.Current.CancellationToken);
        var keyword = await context.FileClassificationKeywords.SingleAsync(TestContext.Current.CancellationToken);
        keyword.CategoryId.ShouldBe(category.Id);
    }

    [Fact]
    public async Task when_seeding_file_classifications_twice_then_the_second_run_does_not_duplicate_rows()
    {
        var logger = new LoggerConfiguration().CreateLogger();

        await DataSeed.SeedFileClassificationsAsync(csvPath, logger, context);
        await DataSeed.SeedFileClassificationsAsync(csvPath, logger, context);

        int categoryCount = await context.FileClassificationCategories.CountAsync(c => c.Name == "Animals", TestContext.Current.CancellationToken);
        int keywordCount = await context.FileClassificationKeywords.CountAsync(TestContext.Current.CancellationToken);
        categoryCount.ShouldBe(1);
        keywordCount.ShouldBe(1);
    }
}
