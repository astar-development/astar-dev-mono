using AStar.Dev.OneDrive.Sync.Client.Infrastructure;
using AStar.Dev.Utilities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data;

/// <summary>
/// Resolves the SQLite database path at runtime using the same
/// platform-appropriate directory as the token cache.
/// </summary>
public static class DbContextFactory
{
    public static AppDbContext Create()
    {
        string dbPath = ApplicationMetadata.ApplicationNameLowered.ApplicationDirectory().CombinePath($"{ApplicationMetadata.ApplicationNameLowered}.db");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new AppDbContext(options);
    }
}
