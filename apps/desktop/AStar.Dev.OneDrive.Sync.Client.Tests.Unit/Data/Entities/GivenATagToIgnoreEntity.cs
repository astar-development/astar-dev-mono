using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenATagToIgnoreEntity : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;

    public GivenATagToIgnoreEntity()
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
    public async Task when_a_tag_to_ignore_is_added_then_it_can_be_retrieved()
    {
        context.TagsToIgnore.Add(new TagToIgnoreEntity { Value = "nsfw", IgnoreImage = true });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.TagsToIgnore.First();

        retrieved.Value.ShouldBe("nsfw");
        retrieved.IgnoreImage.ShouldBeTrue();
    }
}
