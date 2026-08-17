using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Data.Entities;

public sealed class GivenAPersistedFileClassificationCategory : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;
    private bool disposed;

    public GivenAPersistedFileClassificationCategory()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        context = new AppDbContext(options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (!disposing)
            return;

        context.Dispose();
        connection.Dispose();
    }

    [Fact]
    public async Task when_a_category_is_added_then_it_can_be_retrieved()
    {
        context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "sunset", ParentId = 1, IncludeInSearch = true });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.FileClassificationCategories.Single(c => c.Name == "sunset");

        retrieved.ParentId.ShouldBe(1);
        retrieved.IncludeInSearch.ShouldBeTrue();
    }

    [Fact]
    public async Task when_two_categories_share_a_name_then_both_are_saved()
    {
        context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "duplicate" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "duplicate" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.FileClassificationCategories.Count(c => c.Name == "duplicate").ShouldBe(2);
    }
}
