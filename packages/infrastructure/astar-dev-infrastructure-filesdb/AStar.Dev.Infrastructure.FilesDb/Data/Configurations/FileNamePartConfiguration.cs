using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Data.Configurations;

/// <summary>
/// </summary>
public class FileNamePartConfiguration : IEntityTypeConfiguration<FileNamePart>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileNamePart> builder)
    {
        _ = builder
            .ToTable(nameof(FileNamePart), Constants.SchemaName)
            .HasKey(fileNamePart => fileNamePart.Id);

        _ = builder.Property(fileNamePart => fileNamePart.Text).HasMaxLength(150);
    }
}