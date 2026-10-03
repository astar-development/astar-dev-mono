using AStarDev.ControlDb.FileDetail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.Files;

/// <summary>EF Core configuration for <see cref="IgnoredWallpaperEntity"/>.</summary>
public sealed class IgnoredWallpaperEntityConfiguration : IEntityTypeConfiguration<IgnoredWallpaperEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IgnoredWallpaperEntity> builder)
    {
        _ = builder.ToTable("IgnoredWallpaper");
        _ = builder.HasKey(ignored => ignored.FileHandle);
        _ = builder.Property(ignored => ignored.FileHandle).HasConversion(fileHandle => fileHandle.Value, value => new FileHandle(value)).ValueGeneratedNever();
    }
}
