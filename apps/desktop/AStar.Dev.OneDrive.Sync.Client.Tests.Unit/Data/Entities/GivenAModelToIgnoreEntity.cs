using AStar.Dev.OneDrive.Sync.Client.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenAModelToIgnoreEntity : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;

    public GivenAModelToIgnoreEntity()
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
    public async Task when_a_model_to_ignore_is_added_then_it_can_be_retrieved()
    {
        context.ModelsToIgnore.Add(new ModelToIgnoreEntity { Value = "some-model" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.ModelsToIgnore.First();

        retrieved.Value.ShouldBe("some-model");
    }
}
