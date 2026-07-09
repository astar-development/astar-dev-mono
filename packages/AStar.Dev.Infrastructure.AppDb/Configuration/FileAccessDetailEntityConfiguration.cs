using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="FileAccessDetailEntity"/>.</summary>
public sealed class FileAccessDetailEntityConfiguration : IEntityTypeConfiguration<FileAccessDetailEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileAccessDetailEntity> builder)
    {
        _ = builder.ToTable("FileAccessDetail");
        _ = builder.HasKey(detail => detail.Id);
        _ = builder.Property(detail => detail.FileDetailId).HasConversion(fileId => fileId.Id, guid => new FileId(guid));

        _ = builder.HasOne(detail => detail.FileDetail)
            .WithOne(file => file.FileAccessDetail)
            .HasForeignKey<FileAccessDetailEntity>(detail => detail.FileDetailId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
