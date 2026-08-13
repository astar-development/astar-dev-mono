using AStarDev.ControlDb.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.Files;

/// <summary>
/// Represents the configuration for the FileAccessDetailEntity in the database context.
/// </summary>
public class FileAccessDetailEntityConfiguration : IEntityTypeConfiguration<FileAccessDetailEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<FileAccessDetailEntity> builder)
    {
        builder.ToTable("FileAccessDetails");
        builder.Property(d => d.Id).HasConversion(id => id.Value, value => new FileAccessDetailId(value));
        builder.Property(d => d.FileEntityId).HasConversion(id => id.Value, value => new FileId(value));

        builder.HasOne(d => d.FileDetail)
            .WithOne()
            .HasForeignKey<FileAccessDetailEntity>("FileEntityId")
            .HasPrincipalKey<FileEntity>("Id");
    }
}
