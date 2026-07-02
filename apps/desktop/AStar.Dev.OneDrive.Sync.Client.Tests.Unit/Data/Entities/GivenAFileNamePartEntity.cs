using AStar.Dev.OneDrive.Sync.Client.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenAFileNamePartEntity : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;

    public GivenAFileNamePartEntity()
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
    public async Task when_a_file_name_part_is_added_then_it_can_be_retrieved()
    {
        context.FileNameParts.Add(new FileNamePartEntity { Text = "sunset", IncludeInSearch = true });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.FileNameParts.First();

        retrieved.Text.ShouldBe("sunset");
        retrieved.IncludeInSearch.ShouldBeTrue();
    }
}
