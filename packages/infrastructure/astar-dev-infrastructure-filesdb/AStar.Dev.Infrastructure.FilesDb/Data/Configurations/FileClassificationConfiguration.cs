using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Data.Configurations;

/// <summary>
/// </summary>
public class FileClassificationConfiguration : IEntityTypeConfiguration<FileClassification>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileClassification> builder)
    {
        _ = builder
            .ToTable(nameof(FileClassification), Constants.SchemaName)
            .HasKey(fileClassification => fileClassification.Id);

        _ = builder.HasIndex(fileClassification => fileClassification.Name).IsUnique();

        //_ = builder.HasMany<FileNamePart>();
        _ = builder.Property(fileClassification => fileClassification.Name).HasMaxLength(150);
    }
}