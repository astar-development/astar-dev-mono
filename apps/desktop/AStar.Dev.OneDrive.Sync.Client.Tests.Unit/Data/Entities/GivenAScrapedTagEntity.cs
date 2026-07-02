using AStar.Dev.OneDrive.Sync.Client.Data;
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
        context.ScrapedTags.Add(new ScrapedTagEntity { Value = "sunset", Category = "nature", IncludeInSearch = true });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.ScrapedTags.First();

        retrieved.Value.ShouldBe("sunset");
        retrieved.Category.ShouldBe("nature");
    }

    [Fact]
    public async Task when_two_scraped_tags_share_a_value_then_save_fails()
    {
        context.ScrapedTags.Add(new ScrapedTagEntity { Value = "duplicate" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.ScrapedTags.Add(new ScrapedTagEntity { Value = "duplicate" });

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }
}
