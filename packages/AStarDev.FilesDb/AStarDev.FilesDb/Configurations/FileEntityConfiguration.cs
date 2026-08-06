using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.FilesDb.Configurations;

/// <summary>
/// Represents the configuration for the FileEntity in the database context.
/// </summary>
public class FileEntityConfiguration : IEntityTypeConfiguration<FileEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<FileEntity> builder)
    {
        builder.HasKey(f => new { f.Path, f.Name });
        builder.Property(f => f.Name).IsRequired().HasMaxLength(255);
        builder.Property(f => f.Path).IsRequired().HasMaxLength(1024);
    }
}
