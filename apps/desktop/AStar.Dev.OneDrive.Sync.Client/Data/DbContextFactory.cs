using AStar.Dev.OneDrive.Sync.Client.Infrastructure;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Persistence;
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
        string dir = new LocalApplicationPathsProvider().ApplicationDirectory;

        string dbPath = dir.CombinePath(ApplicationMetadata.ApplicationNameLowered);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new AppDbContext(options);
    }
}
