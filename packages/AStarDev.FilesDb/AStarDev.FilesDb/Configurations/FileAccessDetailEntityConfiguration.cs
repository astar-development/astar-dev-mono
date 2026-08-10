using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.FilesDb.Configurations;

/// <summary>
/// Represents the configuration for the FileAccessDetailEntity in the database context.
/// </summary>
public class FileAccessDetailEntityConfiguration : IEntityTypeConfiguration<FileAccessDetailEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<FileAccessDetailEntity> builder)
    {
        builder.Property(d => d.FileEntityId).HasConversion(id => id.Value, value => new FileId(value));

        builder.HasOne(d => d.FileDetail)
            .WithOne()
            .HasForeignKey<FileAccessDetailEntity>("FileEntityId")
            .HasPrincipalKey<FileEntity>("Id");
    }
}
