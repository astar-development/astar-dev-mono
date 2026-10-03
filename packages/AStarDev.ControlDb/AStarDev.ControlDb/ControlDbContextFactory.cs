using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AStarDev.ControlDb;

/// <summary>
/// Creates <see cref="ControlDbContext"/> instances for design-time tooling (e.g. `dotnet ef migrations add`),
/// which cannot resolve <see cref="DbContextOptions{TContext}"/> through the application's DI container.
/// </summary>
public sealed class ControlDbContextFactory : IDesignTimeDbContextFactory<ControlDbContext>
{
    /// <inheritdoc/>
    public ControlDbContext CreateDbContext(string[] args)
    {
        var tempDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={tempDir}/Scraper/files.db")
            .Options;

        return new ControlDbContext(options);
    }
}
