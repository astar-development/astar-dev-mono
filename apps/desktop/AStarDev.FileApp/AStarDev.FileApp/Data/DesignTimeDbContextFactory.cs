using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AStar.Dev.File.App.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FileAppDbContext>
{
    public FileAppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FileAppDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new FileAppDbContext(options);
    }
}
