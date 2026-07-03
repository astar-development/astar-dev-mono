using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenDataSeed : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private FilesContext context = null!;
    private string csvPath = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FilesContext>().UseSqlite(connection).Options;
        context = new FilesContext(options);
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

        var classification = await context.FileClassifications.SingleAsync(TestContext.Current.CancellationToken);
        classification.Level.ShouldBe(3);
    }
}
