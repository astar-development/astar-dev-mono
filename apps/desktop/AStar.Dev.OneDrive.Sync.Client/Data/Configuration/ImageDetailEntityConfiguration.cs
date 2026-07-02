using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

/// <summary>EF Core configuration for <see cref="ImageDetailEntity"/>.</summary>
public sealed class ImageDetailEntityConfiguration : IEntityTypeConfiguration<ImageDetailEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ImageDetailEntity> builder)
    {
        _ = builder.ToTable("ImageDetail");
        _ = builder.HasKey(image => image.Id);
        _ = builder.Property(image => image.Id).HasConversion(imageId => imageId.Id, guid => new ImageId(guid));
    }
}
