using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.FilesDb.Configurations;

/// <summary>
/// Represents the configuration for the ImageDetailEntity in the database context.
/// </summary>
public class ImageDetailEntityConfiguration : IEntityTypeConfiguration<ImageDetailEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ImageDetailEntity> builder)
    {
        builder.Property(d => d.FileEntityId).HasConversion(id => id.Id, value => new FileId(value));
        builder.HasKey(d => d.FileEntityId);

        builder.HasOne(d => d.FileDetail)
            .WithOne()
            .HasForeignKey<ImageDetailEntity>("FileEntityId")
            .HasPrincipalKey<FileEntity>("Id");
    }
}
