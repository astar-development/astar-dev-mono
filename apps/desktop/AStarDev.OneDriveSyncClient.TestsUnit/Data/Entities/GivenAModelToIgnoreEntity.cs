using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Data.Entities;

public sealed class GivenAModelToIgnoreEntity : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;
    private bool disposed;

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
    public async Task when_a_model_to_ignore_is_added_then_it_can_be_retrieved()
    {
        context.ModelsToIgnore.Add(new ModelToIgnoreEntity { Value = "some-model" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.ModelsToIgnore.First();

        retrieved.Value.ShouldBe("some-model");
    }
}
