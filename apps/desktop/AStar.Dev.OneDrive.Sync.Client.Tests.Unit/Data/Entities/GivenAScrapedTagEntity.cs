using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenAScrapedTagEntity : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;

    public GivenAScrapedTagEntity()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        context = new AppDbContext(options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        context.Dispose();
        connection.Dispose();
    }

    [Fact]
    public async Task when_a_scraped_tag_is_added_then_it_can_be_retrieved()
    {
        context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "sunset", ParentId = 1, IncludeInSearch = true });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.FileClassificationCategories.First();

        retrieved.Name.ShouldBe("sunset");
        retrieved.ParentId.ShouldBe(1);
    }

    [Fact]
    public async Task when_two_scraped_tags_share_a_value_then_save_fails()
    {
        context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "duplicate" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "duplicate" });

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }
}
