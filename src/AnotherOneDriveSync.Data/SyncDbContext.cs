using AnotherOneDriveSync.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnotherOneDriveSync.Data;

public class SyncDbContext : DbContext
{
    public SyncDbContext(DbContextOptions<SyncDbContext> options) : base(options)
    {
    }

    protected SyncDbContext()
    {
    }

    public virtual DbSet<TokenCacheEntry> TokenCacheEntries { get; set; }
    public virtual DbSet<SyncFolder> SyncFolders { get; set; }
    public virtual DbSet<DriveItemMetadata> DriveItemMetadata { get; set; }
    public virtual DbSet<SyncFolderStatus> SyncFolderStatuses { get; set; }
    public virtual DbSet<AppSettings> AppSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TokenCacheEntryConfiguration());
        modelBuilder.ApplyConfiguration(new SyncFolderConfiguration());
        modelBuilder.ApplyConfiguration(new DriveItemMetadataConfiguration());
        modelBuilder.ApplyConfiguration(new SyncFolderStatusConfiguration());
        modelBuilder.ApplyConfiguration(new AppSettingsConfiguration());
    }
}
