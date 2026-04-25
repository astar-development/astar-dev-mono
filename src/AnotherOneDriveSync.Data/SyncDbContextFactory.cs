using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AnotherOneDriveSync.Data;

public class SyncDbContextFactory : IDesignTimeDbContextFactory<SyncDbContext>
{
    public SyncDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SyncDbContext>();
        optionsBuilder.UseSqlite("Data Source=sync.db");

        return new SyncDbContext(optionsBuilder.Options);
    }
}
