using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data;

public sealed class GivenAnAppDbContext : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;

    public GivenAnAppDbContext()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        context = new AppDbContext(options);
        context.Database.EnsureCreated();
        foreach (var classification in context.FileClassificationCategories)
        {
            context.FileClassificationCategories.Remove(classification);
        }
        context.SaveChanges();
    }

    public void Dispose()
    {
        context.Dispose();
        connection.Dispose();
    }

    [Fact]
    public void when_querying_file_classification_categories_then_dbset_is_accessible()
    {
        var result = context.FileClassificationCategories.ToList();

        result.ShouldNotBeNull();
    }

    [Fact]
    public void when_querying_file_classifications_then_dbset_is_accessible()
    {
        var result = context.FileClassifications.ToList();

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_file_classification_category_added_then_it_can_be_retrieved()
    {
        context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Photos", Level = 1 });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.FileClassificationCategories.First();

        retrieved.Name.ShouldBe("Photos");
    }

    [Fact]
    public async Task when_file_classification_added_then_it_can_be_retrieved()
    {
        var fileDetail = new FileDetailEntity { FileName = new FileName("file.jpg"), DirectoryName = new DirectoryName("/local"), FileHandle = new FileHandle("handle-app-db") };
        context.Files.Add(fileDetail);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var category = new FileClassificationCategoryEntity { Name = "Photos", Level = 1 };
        context.FileClassificationCategories.Add(category);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.FileClassifications.First();

        retrieved.FileDetailId.ShouldBe(fileDetail.Id);
        retrieved.CategoryId.ShouldBe(category.Id);
    }
}
