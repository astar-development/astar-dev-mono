using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

internal sealed class ImageDetailConfiguration : IEntityTypeConfiguration<ImageDetail>
{
    public void Configure(EntityTypeBuilder<ImageDetail> builder)
    {
        _ = builder
           .ToTable(nameof(ImageDetail), Constants.SchemaName)
           .HasKey(imageDetail => imageDetail.Id);
        _ = builder.Property(image => image.Width).HasColumnName("ImageWidth");
        _ = builder.Property(image => image.Height).HasColumnName("ImageHeight");

        _ = builder.Property(imageDetail => imageDetail.Id)
                   .HasConversion(imageDetail => imageDetail.Value, imageDetail => new ImageId(imageDetail));
    }
}
