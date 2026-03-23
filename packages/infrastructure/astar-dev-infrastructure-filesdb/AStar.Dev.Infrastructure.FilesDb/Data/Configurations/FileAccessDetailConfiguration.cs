using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Data.Configurations;

/// <summary>
/// </summary>
public class FileAccessDetailConfiguration : IEntityTypeConfiguration<FileAccessDetail>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileAccessDetail> builder)
        => _ = builder
               .ToTable(nameof(FileAccessDetail), Constants.SchemaName)
               .HasKey(fileAccessDetail => fileAccessDetail.Id);
}