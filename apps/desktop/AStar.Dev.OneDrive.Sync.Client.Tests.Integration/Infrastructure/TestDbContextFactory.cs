using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Infrastructure;

public sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>, IAsyncDisposable
{
    private readonly SqliteConnection keepAliveConnection;
    private readonly string connectionString;

    public TestDbContextFactory()
    {
        connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        keepAliveConnection = new SqliteConnection(connectionString);
        keepAliveConnection.Open();
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    public async ValueTask DisposeAsync() => await keepAliveConnection.DisposeAsync();
}
