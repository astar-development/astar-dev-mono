using AStarDev.ControlDb.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.Files;

/// <summary>
/// Represents the configuration for the ImageDetailEntity in the database context.
/// </summary>
public class ImageDetailEntityConfiguration : IEntityTypeConfiguration<ImageDetailEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ImageDetailEntity> builder)
    {
        builder.ToTable("ImageDetails");
        builder.Property(d => d.Id).HasConversion(id => id.Value, value => new ImageDetailId(value));
        builder.Property(d => d.FileEntityId).HasConversion(id => id.Value, value => new FileId(value));
        builder.HasKey(d => d.Id);

        builder.HasOne(d => d.FileDetail)
            .WithOne(fileEntity => fileEntity.ImageDetail)
            .HasForeignKey<ImageDetailEntity>(imageDetail => imageDetail.FileEntityId)
            .HasPrincipalKey<FileEntity>(fileEntity => fileEntity.Id);
    }
}
