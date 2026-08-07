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
        _ = builder.Property(file => file.Id).HasConversion(fileId => fileId.Value, guid => new FileId(guid));

        _ = builder.ComplexProperty(file => file.FileName, fileName => fileName.Property(name => name.Value).HasColumnName("FileName"));
        _ = builder.ComplexProperty(file => file.DirectoryName, directoryName => directoryName.Property(name => name.Value).HasColumnName("DirectoryName"));

        _ = builder.Property(file => file.FileHandle).HasConversion(fileHandle => fileHandle.Value, value => new FileHandle(value));
        _ = builder.HasIndex(file => file.FileHandle).IsUnique();
        _ = builder.HasIndex(file => file.FileSize);
        _ = builder.HasIndex(file => new { file.IsImage, file.FileSize }).HasDatabaseName("IX_FileDetail_DuplicateImages");

    }
}
