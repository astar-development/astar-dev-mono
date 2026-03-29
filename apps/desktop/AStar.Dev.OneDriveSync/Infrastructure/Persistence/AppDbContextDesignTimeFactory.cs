using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Provides a configured <see cref="AppDbContext" /> for EF Core design-time tools
///     (<c>dotnet ef migrations add</c>, <c>dotnet ef database update</c>) — AC DB-03.
///
///     This class is never instantiated at runtime; it exists solely to satisfy the
///     EF Core tooling contract when the DI container is not running.
/// </summary>
internal sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=design-time.db")
            .Options;

        return new AppDbContext(options);
    }
}
