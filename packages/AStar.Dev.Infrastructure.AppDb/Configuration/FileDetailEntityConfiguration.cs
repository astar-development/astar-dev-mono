using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="FileDetailEntity"/>.</summary>
public sealed class FileDetailEntityConfiguration : IEntityTypeConfiguration<FileDetailEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileDetailEntity> builder)
    {
        _ = builder.ToTable("FileDetail");
        _ = builder.HasKey(file => file.Id);
        _ = builder.Property(file => file.Id).HasConversion(fileId => fileId.Id, guid => new FileId(guid));

        _ = builder.ComplexProperty(file => file.FileName, fileName => fileName.Property(name => name.Value).HasColumnName("FileName"));
        _ = builder.ComplexProperty(file => file.DirectoryName, directoryName => directoryName.Property(name => name.Value).HasColumnName("DirectoryName"));

        _ = builder.Property(file => file.FileHandle).HasConversion(fileHandle => fileHandle.Value, value => new FileHandle(value));
        _ = builder.Property(file => file.ImageDetailId).HasConversion(imageId => imageId.Id, guid => new ImageId(guid));

        _ = builder.HasIndex(file => file.FileHandle).IsUnique();
        _ = builder.HasIndex(file => file.FileSize);
        _ = builder.HasIndex(file => new { file.IsImage, file.FileSize }).HasDatabaseName("IX_FileDetail_DuplicateImages");

        _ = builder.HasOne(file => file.FileAccessDetail).WithMany().HasForeignKey(file => file.FileAccessDetailId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        _ = builder.HasOne(file => file.ImageDetail).WithMany().HasForeignKey(file => file.ImageDetailId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        _ = builder.HasOne(file => file.DeletionStatus).WithMany().HasForeignKey(file => file.DeletionStatusId).IsRequired().OnDelete(DeleteBehavior.Cascade);
    }
}
