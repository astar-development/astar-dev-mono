using AStar.Dev.File.App.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.File.App.Data;

public class FileAppDbContext(DbContextOptions<FileAppDbContext> options) : DbContext(options)
{
    public DbSet<ScannedFile> ScannedFiles => Set<ScannedFile>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScannedFile>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.FullPath).IsUnique();
            entity.HasIndex(e => e.SizeInBytes);

            entity.Property(e => e.FileType)
                  .HasConversion<string>();
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
        });
    }
}
