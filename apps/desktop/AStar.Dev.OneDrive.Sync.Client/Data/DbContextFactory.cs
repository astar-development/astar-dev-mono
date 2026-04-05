using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data;

/// <summary>
/// Resolves the SQLite database path at runtime using the same
/// platform-appropriate directory as the token cache.
/// </summary>
public static class DbContextFactory
{
    private const string AppName    = "AStar.Dev.OneDrive.Sync";
    private const string DbFileName = "onedrivesync.db";

    public static AppDbContext Create()
    {
        string dir = GetPlatformDataDirectory();
        _ = Directory.CreateDirectory(dir);

        string dbPath = Path.Combine(dir, DbFileName);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new AppDbContext(options);
    }

    public static string GetPlatformDataDirectory()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return OperatingSystem.IsWindows()
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppName)
            : OperatingSystem.IsMacOS()
                ? Path.Combine(home, "Library", "Application Support", AppName)
                : Path.Combine(home, ".config", AppName);
    }
}
