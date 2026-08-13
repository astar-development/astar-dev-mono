using AStarDev.FilesDb.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.FilesDb.Configurations.Files;

/// <summary>
/// Represents the configuration for the FileEntity in the database context.
/// </summary>
public class FileEntityConfiguration : IEntityTypeConfiguration<FileEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<FileEntity> builder)
    {
        builder.ToTable("Files");
        builder.Property(d => d.Id).HasConversion(id => id.Value, value => new FileId(value));

        builder.HasAlternateKey(f => new { f.Path, f.Name });
        builder.Property(f => f.Name).IsRequired().HasMaxLength(255);
        builder.Property(f => f.Name).HasConversion(
            name => name.Name,
            nameString => new FileName(nameString));
        builder.Property(f => f.Path).IsRequired().HasMaxLength(1024);
        builder.Property(f => f.Path).HasConversion(
            path => path.Path,
            pathString => new DirectoryPath(pathString));
        builder.Property(f => f.Handle).IsRequired().HasMaxLength(255);
        builder.Property(f => f.Handle).HasConversion(
            handle => handle.Value,
            valueString => new FileHandle(valueString));
    }
}
