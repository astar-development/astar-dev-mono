using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenAnEventEntity : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;

    public GivenAnEventEntity()
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
    public async Task when_an_event_is_added_then_it_can_be_retrieved()
    {
        context.Set<EventEntity>().Add(new EventEntity
        {
            Type = EventType.Add,
            EventOccurredAt = DateTimeOffset.UtcNow,
            FileName = "wallpaper.jpg",
            DirectoryName = "/pictures",
            Handle = "handle-1",
            FileSize = 1024,
            FileCreated = DateTimeOffset.UtcNow,
            FileLastModified = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.Set<EventEntity>().First();

        retrieved.Type.ShouldBe(EventType.Add);
        retrieved.FileName.ShouldBe("wallpaper.jpg");
    }
}
