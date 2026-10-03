using AStarDev.ControlDb.FileDetail;
using AStarDev.EFCoreSqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.Files;

/// <summary>EF Core configuration for <see cref="ImageDetailEntity"/>.</summary>
public sealed class ImageDetailEntityConfiguration : IEntityTypeConfiguration<ImageDetailEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ImageDetailEntity> builder)
    {
        _ = builder.ToTable("ImageDetail");
        _ = builder.HasKey(image => image.Id);
        _ = builder.Property(image => image.Id).ValueGeneratedOnAdd();
        _ = builder.Property(image => image.Id).HasConversion(imageId => imageId.Value, guid => new ImageId(guid));
        _ = builder.Property(image => image.Id).HasValueGenerator((_, _) => new StrongGuidIdValueGenerator<ImageId>(value => new ImageId(value)));
        _ = builder.Property(image => image.FileId).HasConversion(fileId => fileId.Value, guid => new FileId(guid));

        _ = builder.HasOne(image => image.FileDetail)
            .WithOne(file => file.ImageDetail)
            .HasForeignKey<ImageDetailEntity>(image => image.FileId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
