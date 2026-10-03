using AStarDev.ControlDb.FileDetail;
using AStarDev.EFCoreSqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.Files;

/// <summary>EF Core configuration for <see cref="FileEntity"/>.</summary>
public sealed class FileEntityConfiguration : IEntityTypeConfiguration<FileEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileEntity> builder)
    {
        _ = builder.ToTable("FileDetail");
        _ = builder.HasKey(file => file.Id);
        _ = builder.Property(file => file.Id).ValueGeneratedOnAdd();
        _ = builder.Property(file => file.Id).HasConversion(fileId => fileId.Value, guid => new FileId(guid));
        _ = builder.Property(file => file.Id).HasValueGenerator((_, _) => new StrongGuidIdValueGenerator<FileId>(value => new FileId(value)));

        _ = builder.ComplexProperty(file => file.FileName, fileName => fileName.Property(name => name.Value).HasColumnName("FileName"));
        _ = builder.ComplexProperty(file => file.DirectoryName, directoryName => directoryName.Property(name => name.Value).HasColumnName("DirectoryName"));

        _ = builder.Property(file => file.FileHandle).HasConversion(fileHandle => fileHandle.Value, value => new FileHandle(value));
        _ = builder.HasIndex(file => file.FileHandle).IsUnique();
        _ = builder.HasIndex(file => file.FileSize);
        _ = builder.HasIndex(file => new { file.IsImage, file.FileSize }).HasDatabaseName("IX_FileDetail_DuplicateImages");

    }
}
