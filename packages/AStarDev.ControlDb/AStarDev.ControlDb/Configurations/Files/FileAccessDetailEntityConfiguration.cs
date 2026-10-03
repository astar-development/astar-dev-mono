using AStarDev.ControlDb.FileDetail;
using AStarDev.EFCoreSqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.Files;

/// <summary>EF Core configuration for <see cref="FileAccessDetailEntity"/>.</summary>
public sealed class FileAccessDetailEntityConfiguration : IEntityTypeConfiguration<FileAccessDetailEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileAccessDetailEntity> builder)
    {
        _ = builder.ToTable("FileAccessDetail");
        _ = builder.HasKey(detail => detail.Id);
        _ = builder.Property(detail => detail.Id).ValueGeneratedOnAdd();
        _ = builder.Property(detail => detail.Id).HasConversion(id => id.Value, guid => new FileAccessDetailId(guid));
        _ = builder.Property(detail => detail.Id).HasValueGenerator((_, _) => new StrongGuidIdValueGenerator<FileAccessDetailId>(value => new FileAccessDetailId(value)));
        _ = builder.Property(detail => detail.FileId).HasConversion(fileId => fileId.Value, guid => new FileId(guid));

        _ = builder.HasOne(detail => detail.FileDetail)
            .WithOne(file => file.FileAccessDetail)
            .HasForeignKey<FileAccessDetailEntity>(detail => detail.FileId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
