using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AStar.Dev.Spikes.SqliteSyncState;

/// <summary>
/// Design-time factory used by EF Core tooling (dotnet ef migrations add / update).
/// Uses a fixed path so migrations can be generated without running the full app.
/// </summary>
public class SyncDbContextFactory : IDesignTimeDbContextFactory<SyncDbContext>
{
    public SyncDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new SyncDbContext(options);
    }
}
